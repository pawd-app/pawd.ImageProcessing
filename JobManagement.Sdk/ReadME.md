# JobManagement.Sdk

A .NET SDK in the `pawd.ImageProcessing` solution for enqueuing and managing background jobs.

> ⚠️ **NOTE:** This is a **work-in-progress proof‑of‑concept**, actively in development. Expect frequent changes and occasional breaking updates.

---

## 📦 Installation

Install the latest stable release from NuGet:

```
dotnet add package pawd.JobManagement.SDK
# or in your .csproj:
<PackageReference Include="Jpawd.JobManagement.SDK" Version="x.x.x" />
```

## 📂 Project Structure

All SDK source under `JobManagement.Sdk/`

Contains core interfaces and helpers for job management:

- Enqueuing jobs
- Tracking progress & status updates
- Retry/failure logic and dead-letter handling
- Abstractions over messaging and persistence

## ⚙️ Prerequisites
- [.NET Core / .NET SDK] (check supported frameworks in .csproj)
- A backing system for jobs (e.g., RabbitMQ, Azure Service Bus, database)

🛠️ Basic Usage

```
using JobManagement.Sdk;

# startup
builder.Services.AddJobManagementSystem(options => { options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")); });

# usage
var job = await _jobFactory.CreateJobAsync(
    userGuid: Guid.NewGuid(),
    details: new { ObjectKey = objectKey, Bucket = _s3Settings.BucketName },
    instanceName: "FileProcessor",
    instanceStatus: "FileProcessor.Pending");

```

Use the relevant interfaces (IJobManager, IJobHandler) to consume and process jobs, and optionally plug in persistence or monitoring.