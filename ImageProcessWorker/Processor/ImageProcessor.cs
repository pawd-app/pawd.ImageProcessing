using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using ImageProcessWorker;
using JobManagement.Sdk;
using Jobs.ImageProcess.UploadValidation.models;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Yolov7net;

public interface IImageProcessor
{
    Task ProcessImage(string imagePath, string objectKey, Guid jobGuid);
}

public class ImageProcessor : IImageProcessor
{
    private readonly IYoloNet _yolo;
    private readonly SKPaint _rectPaint;
    private readonly SKPaint _textPaint;
    private readonly IJobFactory _jobFactory;
    private readonly AppOptions _options;
    private readonly IAmazonS3 _s3Client;
    private readonly GarageS3Settings _s3Settings;

    public ImageProcessor(IOptions<AppOptions> options, IAmazonS3 s3Client,
        IOptions<GarageS3Settings> s3Settings, IJobFactory jobFactory)
    {
        _options = options.Value;
        _s3Client = s3Client;
        _s3Settings = s3Settings.Value;
        _jobFactory = jobFactory;
        
        _yolo = new Yolov8("models/yolo12x.onnx", false); //todo make dynamic
        _yolo.SetupYoloDefaultLabels();

        _rectPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 15,
            IsAntialias = true,
            Color = SKColors.Green
        };

        _textPaint = new SKPaint
        {
            TextSize = 128,
            IsAntialias = true,
            Color = SKColors.Red,
            IsStroke = false
        };
    }

    public async Task ProcessImage(string imagePath, string objectKey, Guid jobGuid)
    {
        var job = await _jobFactory.GetJobAsync(jobGuid);
        var jobDetails = JsonSerializer.Deserialize<ValidatedJobDetails>(job.InstanceDetailsJson);
        
        using var image = SKBitmap.Decode(imagePath);
        var predictions = _yolo.Predict(image);
        using var canvas = new SKCanvas(image);

        foreach (var prediction in predictions)
        {
            var score = Math.Round(prediction.Score, 2);
            canvas.DrawRect(prediction.Rectangle, _rectPaint);
            canvas.DrawText($"{prediction?.Label?.Name} ({score})", 
                prediction.Rectangle.MidX + 3, 
                prediction.Rectangle.Top + 23, 
                _textPaint);
        }

        canvas.Flush();

        using var memoryStream = new MemoryStream();
        image.Encode(memoryStream, SKEncodedImageFormat.Jpeg, 100);
        memoryStream.Position = 0; 

        var putRequest = new PutObjectRequest
        {
            BucketName = "pawd-dev-app-data-outcomes",
            Key = objectKey,
            InputStream = memoryStream,
            ContentType = "image/jpeg",
            UseChunkEncoding = false,
            DisablePayloadSigning = false
        };

        jobDetails.YoloPredictions = predictions.Select(x => x.Label.Name).ToList();
        jobDetails.Bucket = "pawd-dev-app-data-outcomes";
        
        await _s3Client.PutObjectAsync(putRequest);
        await _jobFactory.UpdateJobAsync(jobGuid, "FileProcessor.Validated", jobDetails);
        
        Console.WriteLine($"Processed and uploaded to: outcomes/{objectKey}");
    }
}
