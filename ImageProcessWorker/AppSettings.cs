namespace ImageProcessWorker
{
    public class AppOptions
    {
        public int BatchSize { get; init; }
        public ConnectionStrings ConnectionStrings { get; init; }
        public RabbitMq RabbitMq { get; init; }

        public S3Settings S3Settings { get; init; }
    }

    public class ConnectionStrings
    {
        public string DefaultConnection { get; set; }
    }

    public class RabbitMq
    {
        public string HostName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string QueueName { get; set; }
    }

    public class S3Settings
    {
        public string ServiceURL { get; init; }
        public string AccessKey { get; init; }
        public string SecretKey { get; init; }
        public string UploadBucketName { get; init; }
        public string UploadCompleteBucketName { get; set;  }
        public string ImagePredictionOutputBucketName { get; init; }
    }
}