using SkiaSharp;
using Yolov7net;

public class ImageProcessor
{
    private readonly IYoloNet _yolo;
    private readonly SKPaint _rectPaint;
    private readonly SKPaint _textPaint;

    public ImageProcessor(string modelPath)
    {
        _yolo = new Yolov8(modelPath, false);
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

    public void ProcessImage(string imagePath, string outputPath)
    {
        using var image = SKBitmap.Decode(imagePath);
        var predictions = _yolo.Predict(image);
        using var canvas = new SKCanvas(image);

        foreach (var prediction in predictions)
        {
            var score = Math.Round(prediction.Score, 2);
            canvas.DrawRect(prediction.Rectangle, _rectPaint);
            canvas.DrawText($"{prediction.Label.Name} ({score})", prediction.Rectangle.MidX + 3, prediction.Rectangle.Top + 23, _textPaint);
        }

        canvas.Flush();

        using var imageStream = new SKFileWStream(outputPath);
        image.Encode(imageStream, SKEncodedImageFormat.Jpeg, 100);
        Console.WriteLine($"Processed: {outputPath}");
    }
}
