# Base image with necessary dependencies
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Switch to root user to install dependencies
USER root
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libharfbuzz0b \
    libicu-dev \
    && rm -rf /var/lib/apt/lists/*

# Revert to non-root user for application execution
USER app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["ImageProcessWorker/Jobs.ImageProcess.UploadValidation.csproj", "ImageProcessWorker/"]
COPY ["JobManagement.Sdk/JobManagement.Sdk.csproj", "JobManagement.Sdk/"]
COPY ["Jobs.DataAccess/JobManagement.DataAccess.csproj", "Jobs.DataAccess/"]

# Restore dependencies
RUN dotnet restore "./ImageProcessWorker/Jobs.ImageProcess.UploadValidation.csproj"

# Copy everything else
COPY . .

WORKDIR "/src/ImageProcessWorker"

# Use linux-x64 RID to match SkiaSharp native assets
RUN dotnet build "./Jobs.ImageProcess.UploadValidation.csproj" -c $BUILD_CONFIGURATION -r linux-x64 -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "./Jobs.ImageProcess.UploadValidation.csproj" -c $BUILD_CONFIGURATION -r linux-x64 -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV LD_LIBRARY_PATH=/app/runtimes/linux-x64/native:$LD_LIBRARY_PATH


ENTRYPOINT ["dotnet", "Jobs.ImageProcess.UploadValidation.dll"]
