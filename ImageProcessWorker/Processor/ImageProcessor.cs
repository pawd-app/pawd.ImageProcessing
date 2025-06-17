using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using ImageProcessWorker;
using JobManagement.Sdk;
using Jobs.ImageProcess.UploadValidation.models;
using Microsoft.Extensions.Configuration;
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
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _S3Settings;

    public ImageProcessor(IAmazonS3 s3Client, IJobFactory jobFactory, IOptions<S3Settings> garageSettings)
    {
        _s3Client = s3Client;
        _jobFactory = jobFactory;
        _S3Settings = garageSettings.Value;
        
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
            BucketName = _S3Settings.PushToBucketName,
            Key = objectKey,
            InputStream = memoryStream,
            ContentType = "image/jpg",
            UseChunkEncoding = false,
            DisablePayloadSigning = false
        };

        jobDetails.YoloPredictions = predictions.Select(x => x.Label.Name).ToList();
        jobDetails.Bucket = _S3Settings.PushToBucketName;
        
        await _s3Client.PutObjectAsync(putRequest);
        
        
        var resourceUrl = $"{_S3Settings.ServiceURL}/{_S3Settings.PushToBucketName}/{objectKey}";
       
        jobDetails.ImageUrl = resourceUrl;
        await _jobFactory.UpdateJobAsync(jobGuid, "FileProcessor.Validated", jobDetails);
        
        Console.WriteLine($"Processed and uploaded to: {resourceUrl}");
    }
}
