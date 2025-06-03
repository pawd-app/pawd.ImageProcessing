using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using ImageProcessWorker;
using Jobs.DataAccess;
using Jobs.ImageProcess.UploadValidation.models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Jobs.ImageProcess.UploadValidation
{
    internal class Runner
    {
        private readonly AppOptions _options;
        private readonly IAmazonS3 _s3Client;
        private readonly GarageS3Settings _s3Settings;
        private readonly IImageProcessor _imageProcessor;

        public Runner(IOptions<AppOptions> options, IAmazonS3 s3Client,
            IOptions<GarageS3Settings> s3Settings, IImageProcessor imageProcessor)
        {
            _options = options.Value;
            _s3Client = s3Client;
            _s3Settings = s3Settings.Value;
            _imageProcessor = imageProcessor;
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

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

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

                var job = JsonSerializer.Deserialize<Job>(message);
                var jobDetails = JsonSerializer.Deserialize<JobDetails>(job.InstanceDetailsJson);
                
                await ProcessJob(jobDetails, job.JobGuid);

                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            await channel.BasicConsumeAsync(queue: _options.RabbitMq.QueueName,
                                 autoAck: false,
                                 consumer: consumer);

            Console.ReadLine();
        }

        public async Task ProcessJob(JobDetails bucketInfo, Guid jobGuid)
        {
            try
            {
                // Download the file from S3
                var getObjectRequest = new GetObjectRequest
                {
                    BucketName = bucketInfo.Bucket,
                    Key = bucketInfo.ObjectKey
                };

                using var response = await _s3Client.GetObjectAsync(getObjectRequest);
                await using var responseStream = response.ResponseStream;
                // Create a temporary file to store the downloaded image
                var tempInputPath = Path.GetTempFileName();
                var outputPath = $"results/result_{Path.GetFileNameWithoutExtension(bucketInfo.ObjectKey)}.jpg";
            
                try
                {
                    await using (var fileStream = File.Create(tempInputPath))
                    {
                        await responseStream.CopyToAsync(fileStream);
                    }

                    await _imageProcessor.ProcessImage(tempInputPath, outputPath, jobGuid);
                }
                finally
                {
                    if (File.Exists(tempInputPath))
                    {
                        File.Delete(tempInputPath);
                    }
                }
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"S3 Error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
    }
}
