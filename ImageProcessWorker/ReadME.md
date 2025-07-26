# ImageProcessWorker

A background worker service in the `pawd.ImageProcessing` solution that processes image-related jobs.

> ⚠️ **NOTE:** This project is currently a **work-in-progress proof-of-concept** and under very active development. Expect breaking changes and instability.

---

## Config via `appsettings.json`

The worker expects configuration in `appsettings.json` (or via environment variables), for example:

```json
{
  "Version": "1.0",
  "ConnectionStrings": {
    "DefaultConnection": "<your MySQL connection string here>"
  },
  "RabbitMq": {
    "HostName": "<rabbitmq host>",
    "Username": "<rabbitmq user>",
    "Password": "<rabbitmq pass>",
    "QueueName": "<queue name>"
  },
  "S3": {
    "ServiceURL": "<s3-compatible endpoint>",
    "AccessKey": "<access key>",
    "SecretKey": "<secret key>",
    "UploadBucketName": "<input bucket name>",
    "ImagePredictionOutputBucketName": "<output bucket name>"
  }
}
```
Do not commit real credentials—this is a placeholder template.

### 📂 Project Structure
All source code lives under `ImageProcessWorker/`

Entry point is Program.cs

The worker:

- Connects to RabbitMQ and listens on a designated queue
- Retrieves source images from an S3-compatible bucket
- Applies transformations (e.g. resize, filter, watermark)
- Uploads processed images to another S3 bucket

### ⚙️ Prerequisites
[.NET Core / .NET SDK] (see .csproj)

Access to:

- A RabbitMQ server
- A MySQL database (via DefaultConnection)
- An S3-compatible storage endpoint

### 🛠️ Setup & Run
Clone the repo:

```
git clone https://github.com/pawd-app/pawd.ImageProcessing.git
cd pawd.ImageProcessing/ImageProcessWorker
```

Restore & build:

```
dotnet restore
dotnet build
```

Populate appsettings.json (or set env variables like RabbitMq__HostName, S3__UploadBucketName, etc.)

Run the worker:

```
dotnet run
```

### How It Works
Listens on a configured RabbitMQ queue.

For each job:

- Pulls the image from the input bucket
- Executes any registered image operations
- Pushes the result to the output bucket

All connection details (DB, MQ, S3) are controlled purely via configuration.

### Summary
Component: Queue-driven image processing worker

Jobs: RabbitMQ → S3 → process → S3

Status: Proof‑of‑concept; WIP and under active development