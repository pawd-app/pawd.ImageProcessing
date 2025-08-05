# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Allow configuration of build configuration (Release/Debug)
ARG BUILD_CONFIGURATION=Release

# Ensure native libs path is correct for x64
ENV LD_LIBRARY_PATH=/usr/share/dotnet/x64

WORKDIR /src

# Copy project files and restore dependencies
COPY ["ImageProcessWorker/Jobs.ImageProcess.UploadValidation.csproj", "ImageProcessWorker/"]
COPY ["JobManagement.Sdk/JobManagement.Sdk.csproj", "JobManagement.Sdk/"]
COPY ["Jobs.DataAccess/JobManagement.DataAccess.csproj", "Jobs.DataAccess/"]

RUN dotnet restore "ImageProcessWorker/Jobs.ImageProcess.UploadValidation.csproj"

# Copy the rest of the source and build the project
COPY . .
WORKDIR "/src/ImageProcessWorker"
RUN dotnet build "Jobs.ImageProcess.UploadValidation.csproj" \
    -c $BUILD_CONFIGURATION \
    -r linux-x64 \
    -o /app/build

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Install any required native dependencies for image processing
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libfontconfig1 \
        libfreetype6 \
        libharfbuzz0b \
    && rm -rf /var/lib/apt/lists/*

# Copy built artifacts from the build stage
COPY --from=build /app/build .

# Run the worker
ENTRYPOINT ["dotnet", "Jobs.ImageProcess.UploadValidation.dll"]
