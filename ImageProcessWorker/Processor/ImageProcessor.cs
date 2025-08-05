using Amazon.S3;
using Amazon.S3.Model;
using ImageProcessWorker;
using JobManagement.Sdk;
using Jobs.DataAccess;
using Jobs.ImageProcess.UploadValidation.Constants;
using Jobs.ImageProcess.UploadValidation.models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System.Text;
using System.Text.Json;
using Yolov7net;

public interface IImageProcessor
{
    Task ProcessImageAsync(string imagePath, string objectKey, Guid jobGuid);
}

public class ImageProcessor : IImageProcessor, IDisposable
{
    private const int StrokeWidth = 15;
    private const float TextSize = 128f;
    private const int ImageQuality = 100;
    private const float TextOffsetX = 3f;
    private const float TextOffsetY = 23f;
    private const float DefaultConfidenceThreshold = 0.6f;

    private readonly IYoloNet _yolo;
    private readonly SKPaint _rectPaint;
    private readonly SKPaint _textPaint;
    private readonly IJobFactory _jobFactory;
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _s3Settings;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<ImageProcessor> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ImageProcessor(
        IYoloNet yolo,                                       
        IAmazonS3 s3Client,
        IJobFactory jobFactory,
        IOptions<S3Settings> s3Settings,
        ILogger<ImageProcessor> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<ApiSettings> apiSettings)
    {
        _yolo = yolo ?? throw new ArgumentNullException(nameof(yolo));
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
        _s3Settings = s3Settings?.Value ?? throw new ArgumentNullException(nameof(s3Settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _rectPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = StrokeWidth,
            IsAntialias = true,
            Color = SKColors.Green
        };

        _textPaint = new SKPaint
        {
            TextSize = TextSize,
            IsAntialias = true,
            Color = SKColors.Red,
            IsStroke = false
        };
        _httpClientFactory = httpClientFactory;
        _apiSettings = apiSettings.Value;
    }

    public async Task ProcessImageAsync(string imagePath, string objectKey, Guid jobGuid)
    {
        _logger.LogInformation("Starting processing of {ObjectKey} for job {JobGuid}", objectKey, jobGuid);

        Job job = null;
        ValidatedJobDetails details = null;

        try
        {
            job = await _jobFactory.GetJobAsync(jobGuid).ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"Job {jobGuid} not found");
            details = JsonSerializer.Deserialize<ValidatedJobDetails>(job.InstanceDetailsJson)
                        ?? throw new InvalidOperationException("Failed to deserialize job details");

            using var bitmap = SKBitmap.Decode(imagePath)
                         ?? throw new FileNotFoundException($"Cannot decode image at: {imagePath}");

            using var canvas = new SKCanvas(bitmap);
            var predictions = _yolo.Predict(bitmap).ToList();

            _logger.LogInformation("Predictions for {ObjectKey}: {@Predictions}", objectKey,
                predictions.Select(p => new { p.Label?.Name, Score = Math.Round(p.Score, 2) }));

            DrawPredictions(canvas, predictions);

            var isAnimalPresent = predictions.Any(p =>
                p.Score >= DefaultConfidenceThreshold &&
                Constants.AnimalLabels.Contains(p.Label?.Name));

            var targetBucket = isAnimalPresent
                ? _s3Settings.UploadCompleteBucketName
                : _s3Settings.ImagePredictionQuarantineBucketName;

            await UploadProcessedImageAsync(bitmap, objectKey, details, targetBucket).ConfigureAwait(false);
            await UpdateJobWithResultsAsync(jobGuid, objectKey, details, predictions, targetBucket,
                          isAnimalPresent ? Constants.FileValidated : Constants.FileQuarantined)
                .ConfigureAwait(false);

            _logger.LogInformation("Successfully processed {ObjectKey}", objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {ObjectKey}", objectKey);
            if (job != null && details != null)
            {
                await _jobFactory.UpdateJobAsync(jobGuid, Constants.FileProcessingFailed, details).ConfigureAwait(false);
            }
            throw;
        }
    }

    private void DrawPredictions(SKCanvas canvas, IEnumerable<YoloPrediction> predictions)
    {
        foreach (var pred in predictions)
        {
            var scoreText = $"{pred.Label?.Name ?? "Unknown"} ({Math.Round(pred.Score, 2)})";
            canvas.DrawRect(pred.Rectangle, _rectPaint);
            canvas.DrawText(
                scoreText,
                pred.Rectangle.MidX + TextOffsetX,
                pred.Rectangle.Top + TextOffsetY,
                _textPaint);
        }
        canvas.Flush();
    }

    private async Task UploadProcessedImageAsync(SKBitmap bitmap, string key, ValidatedJobDetails details, string bucket)
    {
        await using var stream = new MemoryStream();
        if (!bitmap.Encode(stream, details.ImageFormat, ImageQuality))
            throw new InvalidOperationException("Failed to encode image");

        stream.Position = 0;
        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = stream,
            ContentType = details.ContentType,
            UseChunkEncoding = false,
            DisablePayloadSigning = false
        };

        await _s3Client.PutObjectAsync(request).ConfigureAwait(false);
        _logger.LogDebug("Uploaded {Key} to S3 bucket {Bucket}", key, bucket);
    }

    private async Task UpdateJobWithResultsAsync(
        Guid jobGuid,
        string objectKey,
        ValidatedJobDetails details,
        IEnumerable<YoloPrediction> predictions,
        string bucket,
        string status)
    {
        details.YoloPredictions = predictions.Select(p => p.Label?.Name ?? "Unknown").ToList();
        details.Bucket = bucket;
        var url = $"{_s3Settings.ServiceURL}/{bucket}/{objectKey}";
        details.ImageUrl = $"{_s3Settings.ServiceURL}/{bucket}/{objectKey}";
        _logger.LogInformation("JOB DETIALS:{0}, {1}", jobGuid, details);
        await _jobFactory.UpdateJobAsync(jobGuid, status, details).ConfigureAwait(false);
        _logger.LogInformation("Job {JobGuid} updated with status {Status} and {Count} predictions",
            jobGuid, status, details.YoloPredictions.Count);

        var client = _httpClientFactory.CreateClient();
        _logger.LogInformation(_apiSettings.ApiUrl);
        var updateEndpoint = new Uri(new Uri(_apiSettings.ApiUrl), $"/Files/{jobGuid}/url");
        _logger.LogInformation(updateEndpoint.ToString());
        var payload = JsonSerializer.Serialize(new { Url = url });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PutAsync(updateEndpoint, content, CancellationToken.None).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _rectPaint.Dispose();
        _textPaint.Dispose();
        _yolo.Dispose();
    }
}
