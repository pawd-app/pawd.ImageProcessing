using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace ImageProcessWorker
{
    internal class Runner
    {
        private readonly AppOptions _options;

        public Runner(IOptions<AppOptions> options)
        {
            _options = options.Value;
        }

        public async Task Run(string[] args)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.RabbitMq.HostName,
                UserName = _options.RabbitMq.Username,
                Password = _options.RabbitMq.Password,
                VirtualHost = "/",
                Port = AmqpTcpEndpoint.UseDefaultPort
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: _options.RabbitMq.QueueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                await ProcessJob(message);

                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            await channel.BasicConsumeAsync(queue: _options.RabbitMq.QueueName,
                                 autoAck: false,
                                 consumer: consumer);

            Console.ReadLine();
        }

        public async Task ProcessJob(string inputPath)
        {
            var processor = new ImageProcessor("models/yolo12x.onnx");
            var outputPath = $"results/result_{Path.GetFileNameWithoutExtension(inputPath)}.jpg";
            processor.ProcessImage(inputPath, outputPath);
        }
    }
}
