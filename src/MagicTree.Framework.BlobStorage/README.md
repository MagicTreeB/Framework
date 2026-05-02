# MagicTree.Framework.BlobStorage

A comprehensive blob storage abstraction for managing files with GUID-based identifiers, powered by MinIO.

## Features

✅ **GUID-Based File Management** - All files are stored with auto-generated GUID identifiers
✅ **Upload/Download/Delete Operations** - Complete CRUD operations for blobs
✅ **Metadata Support** - Store and retrieve file metadata (original filename, content type, size, tags)
✅ **Presigned URLs** - Generate temporary direct-access URLs for downloads
✅ **File Validation** - Size limits and extension whitelisting
✅ **Auto Bucket Creation** - Automatically creates storage buckets
✅ **MinIO Integration** - High-performance object storage backend

## Installation

### 1. Add Project Reference

```bash
dotnet add reference ../../Core/MagicTree.Framework.BlobStorage/MagicTree.Framework.BlobStorage.csproj
```

### 2. Configuration (appsettings.json)

```json
{
  "BlobStorage": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "UseSSL": false,
    "DefaultBucket": "files",
    "AutoCreateBuckets": true,
    "MaxFileSizeMB": 100,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".pdf", ".txt", ".docx"],
    "PresignedUrlExpirationHours": 24
  }
}
```

### 3. Register Services (Program.cs)

```csharp
using MagicTree.Framework.BlobStorage.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add blob storage services
builder.Services.AddBlobStorage(builder.Configuration);

var app = builder.Build();
```

## Usage

### Upload a File with Auto-Generated GUID

```csharp
using MagicTree.Framework.BlobStorage.Interfaces;
using MagicTree.Framework.BlobStorage.Models;

public class FileUploadHandler
{
    private readonly IBlobStorageService _blobStorage;
    
    public FileUploadHandler(IBlobStorageService blobStorage)
    {
        _blobStorage = blobStorage;
    }
    
    public async Task<BlobMetadata> UploadFileAsync(
        IFormFile file, 
        CancellationToken ct)
    {
        using var stream = file.OpenReadStream();
        
        var request = new BlobUploadRequest
        {
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileStream = stream,
            Tags = new Dictionary<string, string>
            {
                { "uploaded-by", "user123" },
                { "category", "documents" }
            },
            GeneratePublicUrl = false
        };
        
        // Upload returns metadata with auto-generated GUID
        var metadata = await _blobStorage.UploadAsync(request, ct);
        
        Console.WriteLine($"File uploaded with GUID: {metadata.BlobId}");
        Console.WriteLine($"Original filename: {metadata.OriginalFileName}");
        
        return metadata;
    }
}
```

### Download a File by GUID

```csharp
public async Task<IResult> DownloadFileAsync(Guid blobId, CancellationToken ct)
{
    var result = await _blobStorage.DownloadAsync(blobId, cancellationToken: ct);
    
    if (result == null)
    {
        return Results.NotFound($"File with ID {blobId} not found");
    }
    
    // Return file stream with proper headers
    return Results.File(
        result.Content,
        result.ContentType,
        result.FileName
    );
}
```

### Delete a File by GUID

```csharp
public async Task<IResult> DeleteFileAsync(Guid blobId, CancellationToken ct)
{
    var deleted = await _blobStorage.DeleteAsync(blobId, cancellationToken: ct);
    
    if (!deleted)
    {
        return Results.NotFound($"File with ID {blobId} not found");
    }
    
    return Results.Ok($"File {blobId} deleted successfully");
}
```

### Check if File Exists

```csharp
public async Task<bool> FileExistsAsync(Guid blobId, CancellationToken ct)
{
    return await _blobStorage.ExistsAsync(blobId, cancellationToken: ct);
}
```

### Get File Metadata (without downloading)

```csharp
public async Task<BlobMetadata?> GetFileInfoAsync(Guid blobId, CancellationToken ct)
{
    var metadata = await _blobStorage.GetMetadataAsync(blobId, cancellationToken: ct);
    
    if (metadata != null)
    {
        Console.WriteLine($"Filename: {metadata.OriginalFileName}");
        Console.WriteLine($"Size: {metadata.SizeInBytes} bytes");
        Console.WriteLine($"Type: {metadata.ContentType}");
        Console.WriteLine($"Uploaded: {metadata.UploadedAt}");
    }
    
    return metadata;
}
```

### Generate Presigned URL for Direct Download

```csharp
public async Task<string?> GetDownloadLinkAsync(
    Guid blobId, 
    int expirationHours = 1,
    CancellationToken ct = default)
{
    // Generate a temporary URL that expires after specified hours
    var url = await _blobStorage.GetPresignedUrlAsync(
        blobId, 
        expirationHours, 
        cancellationToken: ct
    );
    
    return url; // Client can use this URL to download directly from MinIO
}
```

## API Integration Example

### Storage API Endpoints

```csharp
// Storage.Api/ApiEndpoints/FileEndpoints.cs
using MagicTree.Framework.BlobStorage.Interfaces;
using MagicTree.Framework.BlobStorage.Models;

public static class FileEndpoints
{
    public static void MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files").WithTags("Files");

        // Upload file - returns GUID
        group.MapPost("/", async (
            HttpContext context,
            IBlobStorageService blobStorage,
            CancellationToken ct) =>
        {
            var form = await context.Request.ReadFormAsync(ct);
            var file = form.Files.FirstOrDefault();
            
            if (file == null || file.Length == 0)
                return Results.BadRequest("No file uploaded");
            
            using var stream = file.OpenReadStream();
            
            var request = new BlobUploadRequest
            {
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileStream = stream
            };
            
            var metadata = await blobStorage.UploadAsync(request, ct);
            
            return Results.Ok(new
            {
                fileId = metadata.BlobId,
                originalName = metadata.OriginalFileName,
                size = metadata.SizeInBytes,
                contentType = metadata.ContentType,
                uploadedAt = metadata.UploadedAt
            });
        })
        .DisableAntiforgery()
        .WithMetadata("UploadFile", "Upload a file", 
            "Uploads a file and returns a GUID identifier");

        // Download file by GUID
        group.MapGet("/{fileId:guid}", async (
            Guid fileId,
            IBlobStorageService blobStorage,
            CancellationToken ct) =>
        {
            var result = await blobStorage.DownloadAsync(fileId, cancellationToken: ct);
            
            if (result == null)
                return Results.NotFound($"File {fileId} not found");
            
            return Results.File(
                result.Content,
                result.ContentType,
                result.FileName
            );
        })
        .WithMetadata("DownloadFile", "Download file by GUID");

        // Get file metadata
        group.MapGet("/{fileId:guid}/metadata", async (
            Guid fileId,
            IBlobStorageService blobStorage,
            CancellationToken ct) =>
        {
            var metadata = await blobStorage.GetMetadataAsync(fileId, cancellationToken: ct);
            
            if (metadata == null)
                return Results.NotFound($"File {fileId} not found");
            
            return Results.Ok(metadata);
        })
        .WithMetadata("GetFileMetadata", "Get file metadata by GUID");

        // Delete file by GUID
        group.MapDelete("/{fileId:guid}", async (
            Guid fileId,
            IBlobStorageService blobStorage,
            CancellationToken ct) =>
        {
            var deleted = await blobStorage.DeleteAsync(fileId, cancellationToken: ct);
            
            if (!deleted)
                return Results.NotFound($"File {fileId} not found");
            
            return Results.Ok($"File {fileId} deleted successfully");
        })
        .WithMetadata("DeleteFile", "Delete file by GUID");

        // Get presigned download URL
        group.MapGet("/{fileId:guid}/download-url", async (
            Guid fileId,
            [FromQuery] int expirationHours = 1,
            IBlobStorageService blobStorage,
            CancellationToken ct) =>
        {
            var url = await blobStorage.GetPresignedUrlAsync(
                fileId, 
                expirationHours, 
                cancellationToken: ct
            );
            
            if (url == null)
                return Results.NotFound($"File {fileId} not found");
            
            return Results.Ok(new { downloadUrl = url, expiresInHours = expirationHours });
        })
        .WithMetadata("GetDownloadUrl", "Get presigned download URL");
    }
}
```

## Benefits

✅ **Simple API** - Just GUID in, file out
✅ **No Path Management** - No need to manage file paths or directory structures
✅ **Type-Safe** - Strongly typed models and interfaces
✅ **Metadata Rich** - Store and retrieve extensive file metadata
✅ **Scalable** - MinIO handles distributed storage
✅ **Flexible** - Support multiple buckets, custom tags, presigned URLs
✅ **Production Ready** - Error handling, logging, validation

## Architecture

```
┌─────────────────────────────────────────────────┐
│  Storage.Api (Your API)                         │
│  - File Upload/Download Endpoints               │
│  - CQRS Handlers                                 │
└─────────────────┬───────────────────────────────┘
                  │ Uses
                  ↓
┌─────────────────────────────────────────────────┐
│  MagicTree.Framework.BlobStorage (This Package)            │
│  - IBlobStorageService Interface                │
│  - MinioBlobStorageService Implementation       │
│  - GUID-Based File Management                   │
└─────────────────┬───────────────────────────────┘
                  │ Uses
                  ↓
┌─────────────────────────────────────────────────┐
│  MinIO Object Storage                            │
│  - Distributed file storage                      │
│  - Buckets: files/{guid}.{ext}                  │
└─────────────────────────────────────────────────┘
```

## Dependencies

- `Minio` (8.0.2+) - MinIO .NET SDK
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Configuration.Binder`
- `Microsoft.Extensions.Options`

## License

MIT License - See LICENSE file for details
