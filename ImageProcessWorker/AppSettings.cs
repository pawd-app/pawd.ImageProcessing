namespace ImageProcessWorker
{
    public class AppOptions
    {
        public int BatchSize { get; set; }
        public ConnectionStrings ConnectionStrings { get; set; }
        public RabbitMq RabbitMq { get; set; }

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
}
