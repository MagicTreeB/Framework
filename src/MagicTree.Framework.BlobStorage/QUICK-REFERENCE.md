# MagicTree.Framework.BlobStorage - Quick Reference

## 📦 Package Overview

**Location**: `Core/MagicTree.Framework.BlobStorage/`

**Purpose**: GUID-based blob storage abstraction for file management with auto-generated identifiers.

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────┐
│  Your API (Storage.Api, etc.)                   │
│  ┌─────────────────────────────────────┐       │
│  │ Upload Endpoint                      │       │
│  │ - Receives IFormFile                 │       │
│  │ - Calls IBlobStorageService          │       │
│  │ - Returns GUID + metadata            │       │
│  └─────────────────────────────────────┘       │
└─────────────────┬───────────────────────────────┘
                  │
                  ↓ Uses
┌─────────────────────────────────────────────────┐
│  MagicTree.Framework.BlobStorage                           │
│  ┌─────────────────────────────────────┐       │
│  │ IBlobStorageService (Interface)     │       │
│  │ - UploadAsync()                     │       │
│  │ - DownloadAsync()                   │       │
│  │ - DeleteAsync()                     │       │
│  │ - GetMetadataAsync()                │       │
│  │ - ExistsAsync()                     │       │
│  │ - GetPresignedUrlAsync()            │       │
│  └─────────────────────────────────────┘       │
│  ┌─────────────────────────────────────┐       │
│  │ MinioBlobStorageService             │       │
│  │ - Auto-generates GUID               │       │
│  │ - Stores as: {GUID}.{extension}     │       │
│  │ - Embeds metadata in object         │       │
│  └─────────────────────────────────────┘       │
└─────────────────┬───────────────────────────────┘
                  │
                  ↓ Uses Minio SDK
┌─────────────────────────────────────────────────┐
│  MinIO Object Storage                            │
│  Bucket: files/                                  │
│  - 123e4567-e89b-12d3-a456-426614174000.jpg     │
│  - 987fcdeb-51a2-43f8-b9c3-123456789abc.pdf     │
│  - a1b2c3d4-e5f6-7890-abcd-ef1234567890.docx    │
└─────────────────────────────────────────────────┘
```

## 📝 Key Concepts

### 1. GUID-Based Naming
- Every file gets a unique GUID identifier
- Stored as: `{GUID}{extension}` (e.g., `123e4567-e89b-12d3-a456-426614174000.jpg`)
- Original filename preserved in metadata
- No path management needed

### 2. Metadata Storage
Metadata is stored as MinIO object headers:
- `x-amz-meta-original-filename`: Original filename
- `x-amz-meta-blob-id`: GUID identifier
- `x-amz-meta-uploaded-at`: Upload timestamp
- Custom tags: `x-amz-meta-{tag-name}`

### 3. Operations

| Operation | Method | Input | Output |
|-----------|--------|-------|--------|
| Upload | `UploadAsync()` | Stream + metadata | `BlobMetadata` (with GUID) |
| Download | `DownloadAsync()` | GUID | `BlobDownloadResult` (stream + metadata) |
| Delete | `DeleteAsync()` | GUID | `bool` (success/failure) |
| Exists | `ExistsAsync()` | GUID | `bool` |
| Metadata | `GetMetadataAsync()` | GUID | `BlobMetadata` or `null` |
| Presigned URL | `GetPresignedUrlAsync()` | GUID | Temporary URL string |

## 🚀 Quick Start

### 1. Add Reference
```bash
cd YourApi
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
    "AllowedExtensions": [".jpg", ".pdf", ".txt"],
    "PresignedUrlExpirationHours": 24
  }
}
```

### 3. Register Services (Program.cs)
```csharp
using MagicTree.Framework.BlobStorage.Extensions;

builder.Services.AddBlobStorage(builder.Configuration);
```

### 4. Use in Code
```csharp
public class FileHandler
{
    private readonly IBlobStorageService _blobStorage;
    
    public async Task<Guid> UploadAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        
        var request = new BlobUploadRequest
        {
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileStream = stream
        };
        
        var metadata = await _blobStorage.UploadAsync(request);
        return metadata.BlobId; // Returns GUID
    }
    
    public async Task<Stream> DownloadAsync(Guid blobId)
    {
        var result = await _blobStorage.DownloadAsync(blobId);
        return result?.Content ?? throw new FileNotFoundException();
    }
}
```

## 📊 Models

### BlobUploadRequest
```csharp
new BlobUploadRequest
{
    OriginalFileName = "document.pdf",      // Required
    ContentType = "application/pdf",         // Required
    FileStream = stream,                     // Required
    BucketName = "custom-bucket",            // Optional (uses default)
    Tags = new() { ["category"] = "docs" }, // Optional
    GeneratePublicUrl = false                // Optional
}
```

### BlobMetadata (Response)
```csharp
{
    BlobId = Guid.NewGuid(),
    OriginalFileName = "document.pdf",
    ContentType = "application/pdf",
    SizeInBytes = 1024000,
    FileExtension = ".pdf",
    BucketName = "files",
    PublicUrl = null,
    UploadedAt = DateTimeOffset.UtcNow,
    Tags = { ["category"] = "docs" }
}
```

### BlobDownloadResult
```csharp
{
    Metadata = { /* BlobMetadata */ },
    Content = Stream,                // File stream
    ContentType = "application/pdf", // For HTTP headers
    FileName = "document.pdf",       // For Content-Disposition
    FileSize = 1024000              // In bytes
}
```

## 🔗 API Endpoints Example

```csharp
// Upload
POST /api/blobs/upload
Content-Type: multipart/form-data
→ { fileId: "GUID", originalName: "...", size: 1024 }

// Download
GET /api/blobs/download/{guid}
→ File stream with headers

// Metadata
GET /api/blobs/{guid}/metadata
→ { fileId, originalName, size, contentType, ... }

// Presigned URL
GET /api/blobs/{guid}/download-url?expirationHours=2
→ { downloadUrl: "http://...", expiresInHours: 2 }

// Delete
DELETE /api/blobs/{guid}
→ { message: "File deleted successfully" }

// Check Exists
HEAD /api/blobs/{guid}
→ 200 OK or 404 Not Found
```

## ✅ Benefits

| Feature | Before (Direct MinIO) | After (BlobStorage) |
|---------|----------------------|---------------------|
| File Naming | Manual path management | Auto GUID generation |
| Metadata | Separate database table | Embedded in blob |
| Download | Lookup name → download | Download by GUID directly |
| Validation | Manual checks | Built-in size/extension validation |
| URLs | Manual presigned URL logic | One method call |
| Testing | Mock MinIO client | Mock IBlobStorageService |
| Backend Switch | Rewrite all code | Implement IBlobStorageService |

## 📚 Files Created

1. `MagicTree.Framework.BlobStorage.csproj` - Project file
2. `Interfaces/IBlobStorageService.cs` - Service interface
3. `Services/MinioBlobStorageService.cs` - MinIO implementation
4. `Models/BlobMetadata.cs` - Metadata model
5. `Models/BlobUploadRequest.cs` - Upload request model
6. `Models/BlobDownloadResult.cs` - Download result model
7. `Options/BlobStorageOptions.cs` - Configuration model
8. `Extensions/BlobStorageExtensions.cs` - DI extensions
9. `README.md` - Full documentation

## 🧪 Testing

```powershell
# Run test script
cd Apis\Storage
.\test-blob-storage.ps1
```

## 🎯 Next Steps

1. ✅ Add project reference to your API
2. ✅ Update appsettings.json
3. ✅ Register services in Program.cs
4. ✅ Create endpoints using BlobEndpoints.cs
5. ✅ Test with PowerShell script
6. ✅ Integrate with your domain models

## 💡 Tips

- **Store GUID in Database**: Save the returned GUID in your domain entities
- **Metadata Tags**: Use tags for categorization, user tracking, etc.
- **Presigned URLs**: Great for direct browser downloads without API
- **File Validation**: Configure `MaxFileSizeMB` and `AllowedExtensions`
- **Multiple Buckets**: Use different buckets for different file types

## 🔧 Troubleshooting

**Issue**: "BlobStorage configuration is missing"
→ **Solution**: Add `BlobStorage` section to appsettings.json

**Issue**: "File size exceeds maximum"
→ **Solution**: Increase `MaxFileSizeMB` in configuration

**Issue**: "File extension not allowed"
→ **Solution**: Add extension to `AllowedExtensions` or leave empty for all

**Issue**: "Cannot connect to MinIO"
→ **Solution**: Verify MinIO is running on configured endpoint

---

**Created**: December 19, 2025
**Version**: 1.0.0
**Package**: MagicTree.Framework.BlobStorage
