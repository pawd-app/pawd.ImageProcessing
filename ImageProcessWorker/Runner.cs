using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using ImageProcessWorker;
using Jobs.DataAccess;
using Jobs.ImageProcess.UploadValidation.models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Jobs.ImageProcess.UploadValidation
{
    internal class Runner
    {
        private readonly AppOptions _options;
        private readonly IAmazonS3 _s3Client;
        private readonly IImageProcessor _imageProcessor;
        private readonly ILogger<Runner> _logger;

        private const int PrefetchCount = 1;
        private const string VirtualHost = "/";

        public Runner(
            IOptions<AppOptions> options, 
            IAmazonS3 s3Client, 
            IImageProcessor imageProcessor,
            ILogger<Runner> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync(string[] args)
        {
            var factory = CreateConnectionFactory();
            
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await SetupQueueAsync(channel);
            await SetupConsumerAsync(channel);

            _logger.LogInformation("Worker started, waiting for messages. Press any key to exit.");
            Console.ReadLine();
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory
            {
                HostName = _options.RabbitMq.HostName,
                UserName = _options.RabbitMq.Username,
                Password = _options.RabbitMq.Password,
                VirtualHost = VirtualHost,
                Port = AmqpTcpEndpoint.UseDefaultPort
            };
        }

        private async Task SetupQueueAsync(IChannel channel)
        {
            await channel.QueueDeclareAsync(
                queue: _options.RabbitMq.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await channel.BasicQosAsync(
                prefetchSize: 0, 
                prefetchCount: PrefetchCount, 
                global: false);
        }

        private async Task SetupConsumerAsync(IChannel channel)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    await ProcessMessageAsync(ea, channel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    // Consider implementing dead letter queue or retry logic
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: _options.RabbitMq.QueueName,
                autoAck: false,
                consumer: consumer);
        }

        private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, IChannel channel)
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogDebug("Received message: {Message}", message);

            var job = DeserializeJob(message);
            var jobDetails = DeserializeJobDetails(job.InstanceDetailsJson);

            await ProcessJobAsync(jobDetails, job.JobGuid);
            await channel.BasicAckAsync(ea.DeliveryTag, false);
            
            _logger.LogInformation("Successfully processed job {JobGuid}", job.JobGuid);
        }

        private static Job DeserializeJob(string message)
        {
            try
            {
                return JsonSerializer.Deserialize<Job>(message) 
                    ?? throw new InvalidOperationException("Failed to deserialize job");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Invalid job message format", ex);
            }
        }

        private static JobDetails DeserializeJobDetails(string instanceDetailsJson)
        {
            try
            {
                return JsonSerializer.Deserialize<JobDetails>(instanceDetailsJson) 
                    ?? throw new InvalidOperationException("Failed to deserialize job details");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Invalid job details format", ex);
            }
        }

        private async Task ProcessJobAsync(JobDetails jobDetails, Guid jobGuid)
        {
            _logger.LogInformation("Processing job {JobGuid} for object {ObjectKey}", 
                jobGuid, jobDetails.ObjectKey);

            var tempFilePath = Path.GetTempFileName();
            
            try
            {
                await DownloadFromS3Async(jobDetails, tempFilePath);
                await _imageProcessor.ProcessImage(tempFilePath, jobDetails.ObjectKey, jobGuid);
                await MoveToCompletedBucketAsync(jobDetails);
                
                _logger.LogInformation("Completed processing job {JobGuid}", jobGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process job {JobGuid}", jobGuid);
                throw;
            }
            finally
            {
                CleanupTempFile(tempFilePath);
            }
        }

        private async Task DownloadFromS3Async(JobDetails jobDetails, string tempFilePath)
        {
            var getObjectRequest = new GetObjectRequest
            {
                BucketName = jobDetails.Bucket,
                Key = jobDetails.ObjectKey
            };

            using var response = await _s3Client.GetObjectAsync(getObjectRequest);
            await using var responseStream = response.ResponseStream;
            await using var fileStream = File.Create(tempFilePath);
            
            await responseStream.CopyToAsync(fileStream);
            
            _logger.LogDebug("Downloaded {ObjectKey} to temp file", jobDetails.ObjectKey);
        }

        private async Task MoveToCompletedBucketAsync(JobDetails jobDetails)
        {
            await CopyToCompletedBucketAsync(jobDetails);
            await DeleteFromSourceBucketAsync(jobDetails);
        }

        private async Task CopyToCompletedBucketAsync(JobDetails jobDetails)
        {
            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = jobDetails.Bucket,
                SourceKey = jobDetails.ObjectKey,
                DestinationBucket = _options.S3Settings.UploadCompleteBucketName,
                DestinationKey = jobDetails.ObjectKey
            };

            await _s3Client.CopyObjectAsync(copyRequest);
            _logger.LogDebug("Copied {ObjectKey} to completed bucket", jobDetails.ObjectKey);
        }

        private async Task DeleteFromSourceBucketAsync(JobDetails jobDetails)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = jobDetails.Bucket,
                Key = jobDetails.ObjectKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
            _logger.LogDebug("Deleted {ObjectKey} from source bucket", jobDetails.ObjectKey);
        }

        private void CleanupTempFile(string tempFilePath)
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    _logger.LogDebug("Cleaned up temp file {TempFilePath}", tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temp file {TempFilePath}", tempFilePath);
            }
        }
    }
}
