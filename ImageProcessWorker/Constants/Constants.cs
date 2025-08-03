namespace Jobs.ImageProcess.UploadValidation.Constants
{
    internal static class Constants
    {
        public static readonly HashSet<string> AnimalLabels = new HashSet<string>
        {
            "cat",
            "dog",
            "bird",
            "horse",
            "sheep",
            "cow",
            "elephant",
            "bear",
            "zebra",
            "giraffe"
        };

        public const string FileValidated = "FileProcessor.Validated";
        public const string FileQuarantined = "FileProcessor.Quarantined";

    }
}
