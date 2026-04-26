# MMO.Core.VideoStorage

Local disk-based video storage implementation with GUID-based file management.

## Features

✅ **GUID-based Identification**: Each video gets a unique GUID identifier  
✅ **Local Disk Storage**: Store videos on local filesystem with organized directory structure  
✅ **Metadata Management**: Automatic JSON metadata files for each video  
✅ **File Validation**: Size limits and extension whitelisting  
✅ **Date Organization**: Optional date-based folder structure (yyyy/MM/dd)  
✅ **Pagination**: List videos with pagination support  
✅ **Storage Statistics**: Get total count, size, and average video size  
✅ **Public URL Generation**: Optional base URL for accessing videos  
✅ **Async Operations**: All methods are async with cancellation token support  
✅ **Type-Safe**: Strong typing with required properties

## Installation

Add package reference to your project:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Core\MMO.Core.VideoStorage\MMO.Core.VideoStorage.csproj" />
</ItemGroup>
```

## Configuration

**appsettings.json:**
```json
{
  "VideoStorage": {
    "StorageDirectory": "./videos",
    "MaxFileSizeMB": 500,
    "AllowedExtensions": [".mp4", ".webm", ".avi", ".mov", ".mkv", ".flv", ".wmv"],
    "OrganizeByDate": true,
    "DateFormat": "yyyy/MM/dd",
    "CreateMetadataFile": true,
    "BaseUrl": "https://yourdomain.com",
    "BufferSize": 81920
  }
}
```

## Usage

**1. Register in Program.cs:**
```csharp
using MMO.Core.VideoStorage.Extensions;

// Option 1: From configuration
builder.Services.AddLocalDiskVideoStorage(builder.Configuration);

// Option 2: With custom options
builder.Services.AddLocalDiskVideoStorage(options =>
{
    options.StorageDirectory = "./my-videos";
    options.MaxFileSizeMB = 1000;
    options.OrganizeByDate = true;
});
```

**2. Inject and Use in Your Services:**
```csharp
using MMO.Core.VideoStorage.Interfaces;
using MMO.Core.VideoStorage.Models;

public class VideoService
{
    private readonly IVideoStorage _videoStorage;
    
    public VideoService(IVideoStorage videoStorage)
    {
        _videoStorage = videoStorage;
    }
    
    public async Task<VideoMetadata> UploadVideoAsync(Stream videoStream, string fileName)
    {
        var request = new VideoUploadRequest
        {
            OriginalFileName = fileName,
            VideoStream = videoStream,
            ContentType = "video/mp4",
            Title = "My Video",
            Description = "Sample video description",
            UserId = "user123"
        };
        
        return await _videoStorage.UploadAsync(request);
    }
    
    public async Task<VideoDownloadResult?> GetVideoAsync(Guid videoId)
    {
        return await _videoStorage.DownloadAsync(videoId);
    }
    
    public async Task<bool> DeleteVideoAsync(Guid videoId)
    {
        return await _videoStorage.DeleteAsync(videoId);
    }
    
    public async Task<VideoListResult> GetAllVideosAsync(int page = 1, int pageSize = 20)
    {
        return await _videoStorage.ListAsync(page, pageSize);
    }
    
    public async Task<VideoStorageStats> GetStatsAsync()
    {
        return await _videoStorage.GetStorageStatsAsync();
    }
}
```

**3. Controller Example:**
```csharp
[ApiController]
[Route("api/videos")]
public class VideoController : ControllerBase
{
    private readonly IVideoStorage _videoStorage;
    
    public VideoController(IVideoStorage videoStorage)
    {
        _videoStorage = videoStorage;
    }
    
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");
        
        await using var stream = file.OpenReadStream();
        
        var request = new VideoUploadRequest
        {
            OriginalFileName = file.FileName,
            VideoStream = stream,
            ContentType = file.ContentType,
            UserId = User.Identity?.Name
        };
        
        var metadata = await _videoStorage.UploadAsync(request);
        return Ok(metadata);
    }
    
    [HttpGet("{videoId:guid}")]
    public async Task<IActionResult> Download(Guid videoId)
    {
        var result = await _videoStorage.DownloadAsync(videoId);
        if (result == null)
            return NotFound();
        
        return File(result.VideoStream, result.Metadata.ContentType, result.Metadata.OriginalFileName);
    }
    
    [HttpDelete("{videoId:guid}")]
    public async Task<IActionResult> Delete(Guid videoId)
    {
        var deleted = await _videoStorage.DeleteAsync(videoId);
        return deleted ? NoContent() : NotFound();
    }
    
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _videoStorage.ListAsync(page, pageSize);
        return Ok(result);
    }
    
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var stats = await _videoStorage.GetStorageStatsAsync();
        return Ok(stats);
    }
}
```

## API Methods

| Method | Description | Returns |
|--------|-------------|---------|
| `UploadAsync(request)` | Upload a video file | `VideoMetadata` |
| `DownloadAsync(videoId)` | Download video by GUID | `VideoDownloadResult?` |
| `DeleteAsync(videoId)` | Delete video by GUID | `bool` |
| `ExistsAsync(videoId)` | Check if video exists | `bool` |
| `GetMetadataAsync(videoId)` | Get video metadata only | `VideoMetadata?` |
| `ListAsync(page, pageSize)` | List videos with pagination | `VideoListResult` |
| `GetStorageStatsAsync()` | Get storage statistics | `VideoStorageStats` |

## Models

**VideoUploadRequest:**
- `OriginalFileName` (required)
- `VideoStream` (required)
- `ContentType` (required)
- `Title`, `Description` (optional)
- `DurationSeconds`, `Width`, `Height` (optional)
- `Tags`, `UserId` (optional)

**VideoMetadata:**
- `VideoId` (GUID)
- `OriginalFileName`, `ContentType`, `SizeInBytes`
- `FileExtension`, `FilePath`, `PublicUrl`
- `UploadedAt`, `DurationSeconds`, `Width`, `Height`
- `Title`, `Description`, `Tags`, `UserId`, `ThumbnailUrl`

**VideoDownloadResult:**
- `Metadata` (VideoMetadata)
- `VideoStream` (Stream)
- Implements `IDisposable`

**VideoListResult:**
- `Videos` (List<VideoMetadata>)
- `TotalCount`, `PageNumber`, `PageSize`
- `TotalPages`, `HasNextPage`, `HasPreviousPage`

**VideoStorageStats:**
- `TotalVideoCount`, `TotalSizeInBytes`
- `TotalSizeInMB`, `TotalSizeInGB`
- `AverageSizeInBytes`, `CalculatedAt`

## Directory Structure

**With OrganizeByDate = true:**
```
./videos/
  2025/
    12/
      22/
        {guid}.mp4
        {guid}.meta.json
        {guid2}.webm
        {guid2}.meta.json
```

**With OrganizeByDate = false:**
```
./videos/
  {guid}.mp4
  {guid}.meta.json
  {guid2}.webm
  {guid2}.meta.json
```

## Metadata File Format

Each video has a `.meta.json` file with complete metadata:

```json
{
  "VideoId": "550e8400-e29b-41d4-a716-446655440000",
  "OriginalFileName": "sample.mp4",
  "ContentType": "video/mp4",
  "SizeInBytes": 10485760,
  "FileExtension": ".mp4",
  "FilePath": "./videos/2025/12/22/550e8400-e29b-41d4-a716-446655440000.mp4",
  "PublicUrl": "https://yourdomain.com/videos/550e8400-e29b-41d4-a716-446655440000.mp4",
  "UploadedAt": "2025-12-22T10:30:00Z",
  "DurationSeconds": 120.5,
  "Width": 1920,
  "Height": 1080,
  "Title": "My Video",
  "Description": "Sample video description",
  "Tags": {
    "category": "tutorial",
    "language": "en"
  },
  "UserId": "user123"
}
```

## Error Handling

The service throws exceptions for:
- File size exceeding `MaxFileSizeMB`
- Disallowed file extensions
- File system errors (disk full, permissions)
- Metadata read/write errors

Always wrap operations in try-catch blocks:

```csharp
try
{
    var metadata = await _videoStorage.UploadAsync(request);
    return Ok(metadata);
}
catch (InvalidOperationException ex)
{
    return BadRequest(ex.Message);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Upload failed");
    return StatusCode(500, "Internal server error");
}
```

## Performance Considerations

- **Buffer Size**: Default 80KB buffer for file I/O operations
- **Async I/O**: All file operations use async APIs
- **Pagination**: List method supports pagination to avoid loading all metadata at once
- **Metadata Caching**: Consider caching frequently accessed metadata
- **Large Files**: For videos > 100MB, consider chunked upload/download

## Best Practices

1. ✅ Always dispose `VideoDownloadResult` after use
2. ✅ Validate file size on client-side before upload
3. ✅ Use pagination when listing videos
4. ✅ Enable `CreateMetadataFile` for production
5. ✅ Configure `BaseUrl` for public video access
6. ✅ Set appropriate `MaxFileSizeMB` based on your needs
7. ✅ Use date organization for large video collections
8. ✅ Implement retry logic for transient file system errors

## Limitations

- ❌ No built-in video transcoding
- ❌ No thumbnail generation
- ❌ No streaming support (full download only)
- ❌ No CDN integration
- ❌ No cloud storage support (local disk only)

## Future Enhancements

- 📝 Video transcoding to multiple formats
- 📝 Automatic thumbnail generation
- 📝 Streaming support (HLS/DASH)
- 📝 Cloud storage implementations (Azure Blob, AWS S3)
- 📝 CDN integration
- 📝 Video duration extraction
- 📝 Subtitle file support

## Dependencies

- Microsoft.Extensions.Configuration.Binder (10.0.0)
- Microsoft.Extensions.DependencyInjection.Abstractions (10.0.0)
- Microsoft.Extensions.Logging.Abstractions (10.0.0)
- Microsoft.Extensions.Options (10.0.0)

## License

MIT License - See LICENSE file for details
