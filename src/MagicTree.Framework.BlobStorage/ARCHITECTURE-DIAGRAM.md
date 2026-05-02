# MagicTree.Framework.BlobStorage - Visual Architecture Diagram

## 📐 System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    CLIENT APPLICATION                           │
│  (Browser, Mobile App, API Consumer)                            │
└────────────────┬────────────────────────────────────────────────┘
                 │
                 │ HTTP Request
                 ↓
┌─────────────────────────────────────────────────────────────────┐
│                   STORAGE.API (Your API)                        │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  POST /api/blobs/upload                                    │ │
│  │  - Receives: IFormFile                                     │ │
│  │  - Returns: GUID + Metadata                                │ │
│  └───────────────┬───────────────────────────────────────────┘ │
│                  │                                               │
│  ┌───────────────▼───────────────────────────────────────────┐ │
│  │  BlobEndpoints.cs (8 Endpoints)                            │ │
│  │  - Upload (single/bulk)                                    │ │
│  │  - Download                                                │ │
│  │  - Delete                                                  │ │
│  │  - Get Metadata                                            │ │
│  │  - Get Presigned URL                                       │ │
│  │  - Check Exists                                            │ │
│  └───────────────┬───────────────────────────────────────────┘ │
└──────────────────┼───────────────────────────────────────────────┘
                   │
                   │ Injects IBlobStorageService
                   ↓
┌─────────────────────────────────────────────────────────────────┐
│              MMO.CORE.BLOBSTORAGE (This Package)                │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  IBlobStorageService (Interface)                           │ │
│  │  ┌───────────────────────────────────────────────────┐   │ │
│  │  │ UploadAsync(request)                              │   │ │
│  │  │   → Returns: BlobMetadata (with GUID)            │   │ │
│  │  │                                                    │   │ │
│  │  │ DownloadAsync(guid)                               │   │ │
│  │  │   → Returns: BlobDownloadResult (stream + meta)  │   │ │
│  │  │                                                    │   │ │
│  │  │ DeleteAsync(guid)                                 │   │ │
│  │  │   → Returns: bool (success/failure)               │   │ │
│  │  │                                                    │   │ │
│  │  │ GetMetadataAsync(guid)                            │   │ │
│  │  │   → Returns: BlobMetadata (without stream)       │   │ │
│  │  │                                                    │   │ │
│  │  │ GetPresignedUrlAsync(guid, expiration)            │   │ │
│  │  │   → Returns: string (temporary URL)               │   │ │
│  │  │                                                    │   │ │
│  │  │ ExistsAsync(guid)                                 │   │ │
│  │  │   → Returns: bool                                 │   │ │
│  │  └───────────────────────────────────────────────────┘   │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  MinioBlobStorageService (Implementation)                 │ │
│  │                                                            │ │
│  │  1. Generate GUID: Guid.NewGuid()                         │ │
│  │     → Example: 123e4567-e89b-12d3-a456-426614174000       │ │
│  │                                                            │ │
│  │  2. Create Object Name: {GUID}{extension}                 │ │
│  │     → Example: 123e4567-e89b-12d3-a456-426614174000.jpg   │ │
│  │                                                            │ │
│  │  3. Upload to MinIO with Metadata Headers:                │ │
│  │     - x-amz-meta-original-filename                        │ │
│  │     - x-amz-meta-blob-id                                  │ │
│  │     - x-amz-meta-uploaded-at                              │ │
│  │     - x-amz-meta-{custom-tags}                            │ │
│  │                                                            │ │
│  │  4. Return Metadata Object                                │ │
│  └───────────────┬───────────────────────────────────────────┘ │
└──────────────────┼───────────────────────────────────────────────┘
                   │
                   │ MinIO SDK (PutObjectAsync, GetObjectAsync, etc.)
                   ↓
┌─────────────────────────────────────────────────────────────────┐
│                   MINIO OBJECT STORAGE                          │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  Bucket: files/                                            │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │ 123e4567-e89b-12d3-a456-426614174000.jpg           │ │ │
│  │  │ - Size: 1024000 bytes                               │ │ │
│  │  │ - Content-Type: image/jpeg                          │ │ │
│  │  │ - Metadata:                                          │ │ │
│  │  │   * original-filename: "vacation-photo.jpg"         │ │ │
│  │  │   * blob-id: "123e4567-e89b-12d3-a456-426614174000" │ │ │
│  │  │   * uploaded-at: "2025-12-19T10:30:00Z"             │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │ 987fcdeb-51a2-43f8-b9c3-123456789abc.pdf           │ │ │
│  │  │ - Size: 2048000 bytes                               │ │ │
│  │  │ - Content-Type: application/pdf                     │ │ │
│  │  │ - Metadata: ...                                      │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │ a1b2c3d4-e5f6-7890-abcd-ef1234567890.docx          │ │ │
│  │  │ - Size: 512000 bytes                                │ │ │
│  │  │ - Content-Type: application/vnd...                  │ │ │
│  │  │ - Metadata: ...                                      │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Default Port: 9000                                             │
│  Management UI: http://localhost:9001                           │
└─────────────────────────────────────────────────────────────────┘
```

## 🔄 Upload Flow Sequence

```
┌────────┐    ┌──────────┐    ┌──────────────┐    ┌─────────────┐
│ Client │    │   API    │    │ BlobStorage  │    │    MinIO    │
└───┬────┘    └────┬─────┘    └──────┬───────┘    └──────┬──────┘
    │              │                  │                   │
    │ 1. POST file │                  │                   │
    │─────────────>│                  │                   │
    │              │                  │                   │
    │              │ 2. UploadAsync() │                   │
    │              │─────────────────>│                   │
    │              │                  │                   │
    │              │                  │ 3. Generate GUID  │
    │              │                  │ 123e4567-...      │
    │              │                  │                   │
    │              │                  │ 4. PutObjectAsync │
    │              │                  │──────────────────>│
    │              │                  │ {GUID}.{ext}      │
    │              │                  │ + metadata        │
    │              │                  │                   │
    │              │                  │ 5. Upload Success │
    │              │                  │<──────────────────│
    │              │                  │                   │
    │              │ 6. BlobMetadata  │                   │
    │              │<─────────────────│                   │
    │              │ (with GUID)      │                   │
    │              │                  │                   │
    │ 7. Response  │                  │                   │
    │<─────────────│                  │                   │
    │ { fileId,    │                  │                   │
    │   metadata } │                  │                   │
    │              │                  │                   │
```

## 🔽 Download Flow Sequence

```
┌────────┐    ┌──────────┐    ┌──────────────┐    ┌─────────────┐
│ Client │    │   API    │    │ BlobStorage  │    │    MinIO    │
└───┬────┘    └────┬─────┘    └──────┬───────┘    └──────┬──────┘
    │              │                  │                   │
    │ 1. GET       │                  │                   │
    │ /blobs/{guid}│                  │                   │
    │─────────────>│                  │                   │
    │              │                  │                   │
    │              │ 2. DownloadAsync │                   │
    │              │    (guid)        │                   │
    │              │─────────────────>│                   │
    │              │                  │                   │
    │              │                  │ 3. StatObject     │
    │              │                  │    (get metadata) │
    │              │                  │──────────────────>│
    │              │                  │                   │
    │              │                  │ 4. Metadata       │
    │              │                  │<──────────────────│
    │              │                  │                   │
    │              │                  │ 5. GetObject      │
    │              │                  │    (download)     │
    │              │                  │──────────────────>│
    │              │                  │                   │
    │              │                  │ 6. File Stream    │
    │              │                  │<──────────────────│
    │              │                  │                   │
    │              │ 7. DownloadResult│                   │
    │              │<─────────────────│                   │
    │              │ (stream + meta)  │                   │
    │              │                  │                   │
    │ 8. File      │                  │                   │
    │<─────────────│                  │                   │
    │              │                  │                   │
```

## 🗃️ Data Storage Pattern

```
┌────────────────────────────────────────────────────────────┐
│                     YOUR DATABASE                          │
│  ┌──────────────────────────────────────────────────────┐ │
│  │  StoredFiles Table                                    │ │
│  │  ┌───────────┬──────────────┬──────────────────────┐ │ │
│  │  │ Id (PK)   │ BlobId       │ OriginalFileName     │ │ │
│  │  ├───────────┼──────────────┼──────────────────────┤ │ │
│  │  │ 1         │ 123e4567-... │ vacation-photo.jpg   │ │ │
│  │  │ 2         │ 987fcdeb-... │ report-2025.pdf      │ │ │
│  │  │ 3         │ a1b2c3d4-... │ contract.docx        │ │ │
│  │  └───────────┴──────────────┴──────────────────────┘ │ │
│  │                                                        │ │
│  │  * Store GUID in BlobId column                        │ │
│  │  * Use GUID to download from MinIO                    │ │
│  │  * No file paths needed                               │ │
│  └──────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────┘
                          │
                          │ GUID Reference
                          ↓
┌────────────────────────────────────────────────────────────┐
│                    MINIO STORAGE                           │
│  ┌──────────────────────────────────────────────────────┐ │
│  │  files/ bucket                                        │ │
│  │  ┌──────────────────────────────────────────────────┐│ │
│  │  │ 123e4567-e89b-12d3-a456-426614174000.jpg         ││ │
│  │  │ [Binary Data: 1MB JPEG]                           ││ │
│  │  └──────────────────────────────────────────────────┘│ │
│  │  ┌──────────────────────────────────────────────────┐│ │
│  │  │ 987fcdeb-51a2-43f8-b9c3-123456789abc.pdf         ││ │
│  │  │ [Binary Data: 2MB PDF]                            ││ │
│  │  └──────────────────────────────────────────────────┘│ │
│  │  ┌──────────────────────────────────────────────────┐│ │
│  │  │ a1b2c3d4-e5f6-7890-abcd-ef1234567890.docx        ││ │
│  │  │ [Binary Data: 512KB DOCX]                         ││ │
│  │  └──────────────────────────────────────────────────┘│ │
│  └──────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────┘
```

## 📦 Package Components

```
┌─────────────────────────────────────────────────────────┐
│       MagicTree.Framework.BlobStorage Package Structure            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────────────────────────────────────────┐ │
│  │  Interfaces/                                      │ │
│  │  └─ IBlobStorageService.cs                        │ │
│  │     - Contract for all operations                 │ │
│  │     - 6 methods (Upload, Download, Delete, etc.)  │ │
│  └──────────────────────────────────────────────────┘ │
│                                                         │
│  ┌──────────────────────────────────────────────────┐ │
│  │  Services/                                        │ │
│  │  └─ MinioBlobStorageService.cs                    │ │
│  │     - MinIO implementation                        │ │
│  │     - GUID generation logic                       │ │
│  │     - Metadata embedding                          │ │
│  └──────────────────────────────────────────────────┘ │
│                                                         │
│  ┌──────────────────────────────────────────────────┐ │
│  │  Models/                                          │ │
│  │  ├─ BlobMetadata.cs (response)                    │ │
│  │  ├─ BlobUploadRequest.cs (input)                  │ │
│  │  └─ BlobDownloadResult.cs (output)                │ │
│  └──────────────────────────────────────────────────┘ │
│                                                         │
│  ┌──────────────────────────────────────────────────┐ │
│  │  Options/                                         │ │
│  │  └─ BlobStorageOptions.cs                         │ │
│  │     - Configuration model                         │ │
│  │     - appsettings.json binding                    │ │
│  └──────────────────────────────────────────────────┘ │
│                                                         │
│  ┌──────────────────────────────────────────────────┐ │
│  │  Extensions/                                      │ │
│  │  └─ BlobStorageExtensions.cs                      │ │
│  │     - AddBlobStorage() registration               │ │
│  │     - DI setup                                    │ │
│  └──────────────────────────────────────────────────┘ │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## 🎯 Key Benefits Visualization

```
┌─────────────────────────────────────────────────────────┐
│              BEFORE (Direct MinIO)                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  API → IMinioClient                                     │
│   ↓                                                     │
│  Generate filename manually (path + name + timestamp)  │
│   ↓                                                     │
│  Store metadata in database table                      │
│   ↓                                                     │
│  Upload to MinIO with custom path                      │
│   ↓                                                     │
│  Query database to find file path for download         │
│   ↓                                                     │
│  Download from MinIO using stored path                 │
│                                                         │
│  Problems:                                              │
│  ❌ Manual path management                             │
│  ❌ Database required for lookups                       │
│  ❌ No standardization across APIs                      │
│  ❌ Hard to test (mock IMinioClient)                    │
│                                                         │
└─────────────────────────────────────────────────────────┘

                          ↓↓↓

┌─────────────────────────────────────────────────────────┐
│          AFTER (MagicTree.Framework.BlobStorage)                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  API → IBlobStorageService                              │
│   ↓                                                     │
│  Auto-generate GUID (no manual naming)                 │
│   ↓                                                     │
│  Metadata embedded in blob (no database lookup needed) │
│   ↓                                                     │
│  Upload with GUID as identifier                        │
│   ↓                                                     │
│  Download by GUID directly (no database query)         │
│                                                         │
│  Benefits:                                              │
│  ✅ Automatic GUID generation                          │
│  ✅ Metadata with the file                             │
│  ✅ Standardized across all APIs                        │
│  ✅ Easy to test (mock IBlobStorageService)             │
│  ✅ Backend agnostic (swap MinIO for S3/Azure)          │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

**Visual Reference Created**: December 19, 2025  
**Use Case**: Understanding MagicTree.Framework.BlobStorage architecture and data flow
