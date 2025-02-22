using SkiaSharp;
using Yolov7net;


var paintRect = new SKPaint
{
    Style = SKPaintStyle.Stroke,
    StrokeWidth = 15,
    IsAntialias = true,
    Color = SKColors.Green
};

var paintText = new SKPaint
{
    TextSize = 128,
    IsAntialias = true,
    Color = SKColors.Red,
    IsStroke = false

};

#region yolov12
using (var yolo = new Yolov8("models/yolo12x.onnx", false))
{
    RunYolo(yolo, "yolov12");
}
#endregion

#region yolov10
using (var yolo = new Yolov8("models/yolov10x.onnx", false))
{
    //RunYolo(yolo, "yolov10");
}
#endregion

#region yolov11
using (var yolo = new Yolov8("models/yolo11x.onnx", false))
{
    RunYolo(yolo, "yolov11");
}
#endregion

#region yolov8
using (var yolo = new Yolov8("models/yolov8x.onnx", false))
{
    RunYolo(yolo, "yolov8");
}
#endregion


void RunYolo(IYoloNet yolo, string remark = "")
{
    yolo.SetupYoloDefaultLabels();
    using var image = SKBitmap.Decode("images/3.jpg");
    var predictions = yolo.Predict(image);

    using var canvas = new SKCanvas(image);
    foreach (var prediction in predictions) 
    {
        double score = Math.Round(prediction.Score, 2);


        canvas.DrawRect(prediction.Rectangle, paintRect);

        var x = prediction.Rectangle.Left + 3;
        var y = prediction.Rectangle.Top + 23;
        Console.WriteLine($"{prediction.Label.Name} ({score}) ");
        canvas.DrawText($"{prediction.Label.Name} ({score}) ", x, y, paintText);
    }


    canvas.Flush();

    using var imageStream = new SKFileWStream($"demo_result_{remark}.jpg");
    image.Encode(imageStream, SKEncodedImageFormat.Jpeg, 100);
    Console.WriteLine($"Done {remark}!");
}