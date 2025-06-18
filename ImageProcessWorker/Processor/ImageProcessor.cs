using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using ImageProcessWorker;
using JobManagement.Sdk;
using Jobs.DataAccess;
using Jobs.ImageProcess.UploadValidation.models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Yolov7net;

public interface IImageProcessor
{
    Task ProcessImage(string imagePath, string objectKey, Guid jobGuid);
}

public class ImageProcessor : IImageProcessor, IDisposable
{
    private readonly IYoloNet _yolo;
    private readonly SKPaint _rectPaint;
    private readonly SKPaint _textPaint;
    private readonly IJobFactory _jobFactory;
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _s3Settings;
    private readonly ILogger<ImageProcessor> _logger;

    private const int StrokeWidth = 15;
    private const float TextSize = 128f;
    private const int ImageQuality = 100;
    private const float TextOffsetX = 3f;
    private const float TextOffsetY = 23f;

    public ImageProcessor(
        IAmazonS3 s3Client, 
        IJobFactory jobFactory, 
        IOptions<S3Settings> s3Settings,
        ILogger<ImageProcessor> logger,
        string modelPath = "models/yolo12x.onnx")
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
        _s3Settings = s3Settings?.Value ?? throw new ArgumentNullException(nameof(s3Settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _yolo = InitializeYolo(modelPath);
        _rectPaint = CreateRectanglePaint();
        _textPaint = CreateTextPaint();
    }

    public async Task ProcessImage(string imagePath, string objectKey, Guid jobGuid)
    {
        _logger.LogInformation("Processing image {ObjectKey} for job {JobGuid}", objectKey, jobGuid);

        var job = await GetJobAsync(jobGuid);
        var jobDetails = DeserializeJobDetails(job.InstanceDetailsJson);
        
        var processedImage = ProcessImageWithPredictions(imagePath);
        var predictions = _yolo.Predict(processedImage.originalImage);
        
        DrawPredictions(processedImage.canvas, predictions);
        
        await SaveProcessedImageAsync(processedImage.originalImage, objectKey, jobDetails);
        await UpdateJobWithResults(jobGuid, objectKey, jobDetails, predictions);
        
        _logger.LogInformation("Successfully processed image {ObjectKey}", objectKey);
    }

    private IYoloNet InitializeYolo(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"YOLO model not found at: {modelPath}");
            
        var yolo = new Yolov8(modelPath, false);
        yolo.SetupYoloDefaultLabels();
        return yolo;
    }

    private static SKPaint CreateRectanglePaint() => new()
    {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = StrokeWidth,
        IsAntialias = true,
        Color = SKColors.Green
    };

    private static SKPaint CreateTextPaint() => new()
    {
        TextSize = TextSize,
        IsAntialias = true,
        Color = SKColors.Red,
        IsStroke = false
    };

    private async Task<Job> GetJobAsync(Guid jobGuid)
    {
        var job = await _jobFactory.GetJobAsync(jobGuid);
        if (job == null)
            throw new InvalidOperationException($"Job {jobGuid} not found");
        return job;
    }

    private static ValidatedJobDetails DeserializeJobDetails(string instanceDetailsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<ValidatedJobDetails>(instanceDetailsJson)
                ?? throw new InvalidOperationException("Failed to deserialize job details");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Invalid job details format", ex);
        }
    }

    private (SKBitmap originalImage, SKCanvas canvas) ProcessImageWithPredictions(string imagePath)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"Image file not found: {imagePath}");

        var image = SKBitmap.Decode(imagePath);
        if (image == null)
            throw new InvalidOperationException($"Failed to decode image: {imagePath}");

        var canvas = new SKCanvas(image);
        return (image, canvas);
    }

    private void DrawPredictions(SKCanvas canvas, IEnumerable<YoloPrediction> predictions)
    {
        foreach (var prediction in predictions)
        {
            DrawPrediction(canvas, prediction);
        }
        canvas.Flush();
    }

    private void DrawPrediction(SKCanvas canvas, YoloPrediction prediction)
    {
        var score = Math.Round(prediction.Score, 2);
        var labelText = $"{prediction.Label?.Name ?? "Unknown"} ({score})";
        
        canvas.DrawRect(prediction.Rectangle, _rectPaint);
        canvas.DrawText(
            labelText,
            prediction.Rectangle.MidX + TextOffsetX,
            prediction.Rectangle.Top + TextOffsetY,
            _textPaint);
    }

    private async Task SaveProcessedImageAsync(SKBitmap image, string objectKey, ValidatedJobDetails jobDetails)
    {
        using var memoryStream = new MemoryStream();
        if (!image.Encode(memoryStream, jobDetails.ImageFormat, ImageQuality))
            throw new InvalidOperationException("Failed to encode processed image");
            
        memoryStream.Position = 0;

        var putRequest = new PutObjectRequest
        {
            BucketName = _s3Settings.ImagePredictionOutputBucketName,
            Key = objectKey,
            InputStream = memoryStream,
            ContentType = jobDetails.ContentType,
            UseChunkEncoding = false,
            DisablePayloadSigning = false
        };

        await _s3Client.PutObjectAsync(putRequest);
        _logger.LogDebug("Uploaded processed image {ObjectKey} to S3", objectKey);
    }

    private async Task UpdateJobWithResults(Guid jobGuid, string objectKey, ValidatedJobDetails jobDetails, IEnumerable<YoloPrediction> predictions)
    {
        jobDetails.YoloPredictions = predictions.Select(p => p.Label?.Name ?? "Unknown").ToList();
        jobDetails.Bucket = _s3Settings.ImagePredictionOutputBucketName;
        jobDetails.ImageUrl = GenerateResourceUrl(objectKey);
        
        await _jobFactory.UpdateJobAsync(jobGuid, "FileProcessor.Validated", jobDetails);
        
        _logger.LogInformation("Updated job {JobGuid} with {PredictionCount} predictions. Resource URL: {ResourceUrl}",
            jobGuid, jobDetails.YoloPredictions.Count, jobDetails.ImageUrl);
    }

    private string GenerateResourceUrl(string objectKey) =>
        $"{_s3Settings.ServiceURL}/{_s3Settings.UploadCompleteBucketName}/{objectKey}";

    public void Dispose()
    {
        _rectPaint?.Dispose();
        _textPaint?.Dispose();
        _yolo?.Dispose();
    }
}