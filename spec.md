# Video Streaming Platform — Project Specification

## 1. Project Overview

### Purpose
A self-hosted video streaming platform where users can upload videos, manage their visibility, and stream them to any browser with automatic adaptive bitrate quality selection. Built entirely from scratch — no third-party video hosting or CDN dependency.

### Core Goals
- Efficient chunked file upload (resumable, supports large files)
- Automatic transcoding into multiple HLS quality tiers
- Adaptive Bitrate Streaming (ABR) — browser auto-selects quality based on available bandwidth
- Public/private visibility control per video
- Clean, maintainable architecture that can scale independently per concern

### Key Features
- User registration and JWT authentication
- Resumable chunked video upload (5 MB chunks, up to 3 parallel)
- Automatic transcoding to 360p / 480p / 720p / 1080p via FFmpeg
- HLS (HTTP Live Streaming) output with 6-second segments
- Adaptive bitrate playback via HLS.js in the browser
- Per-video public/private visibility toggle
- Video metadata editing (title, description)

### Non-Goals (v1)
- Live streaming
- DASH (Dynamic Adaptive Streaming over HTTP) support
- Comments, likes, subscriptions, or social features
- CDN integration (production optimization, deferred)
- Video DRM or content protection
- Refresh token rotation (JWT access token only, 15 min expiry)
- Presigned blob URLs for segment serving (deferred production optimization)

---

## 2. Tech Stack

| Layer | Technology | Version | Rationale |
|-------|-----------|---------|-----------|
| API Services | .NET / C# | 8.0 | LTS, best FFmpeg/DDD ecosystem fit |
| Architecture | DDD + Clean Architecture + CQRS | — | Maintainability, bounded context isolation |
| CQRS Bus | MediatR | 12.x | In-process command/query dispatch |
| Validation | FluentValidation | 11.x | Declarative validators per command |
| Message Queue | RabbitMQ | 3.x | Async service integration |
| MQ Abstraction | MassTransit | 8.x | Retry, dead-letter, consumer registration |
| Database | SQL Server | 2022 | EF Core first-class support, Docker image available |
| ORM | Entity Framework Core | 8.x | Code-first, per-service DbContext |
| Auth | JWT (System.IdentityModel) | — | Stateless, service-independent validation |
| Password Hashing | BCrypt.Net-Next | 4.x | Industry-standard bcrypt |
| API Gateway | YARP (Yet Another Reverse Proxy) | 2.x | .NET-native, config-driven routing |
| Blob Storage | Local File System | — | Abstracted behind IBlobStorageService, swappable |
| Video Processing | FFmpeg + ffprobe | 6.x | Industry standard, free, subprocess invocation |
| Frontend | React + TypeScript | 18.x / 5.x | Component-based, strong typing |
| Frontend Build | Vite | 5.x | Fast HMR, ES modules |
| HLS Player | HLS.js | 1.x | ABR in all browsers; native fallback for Safari |
| Frontend State | Redux Toolkit | 2.x | Auth + upload session state |
| Data Fetching | TanStack Query (React Query) | 5.x | GET caching, refetch, stale-while-revalidate |
| HTTP Client | Axios | 1.x | Interceptors for JWT injection |
| Containerization | Docker + docker-compose | 24.x | Reproducible dev environment |

---

## 3. Microservices Architecture

```
                         ┌──────────────────────────────┐
  React Frontend (3000) ─►    API Gateway / YARP (5000)  │
                         └──────┬──────┬──────┬──────────┘
                                │      │      │
                    ┌───────────┘      │      └──────────────┐
                    ▼                  ▼                      ▼
           ┌───────────────┐  ┌────────────────┐  ┌──────────────────┐
           │ Identity.API  │  │VideoManagement │  │  Streaming.API   │
           │    (5001)     │  │   .API (5002)  │  │     (5003)       │
           └───────────────┘  └───────┬────────┘  └──────────────────┘
                                      │ publish                ▲
                                      │ VideoUploaded          │ consume
                                      ▼                        │ VideoDeleted
                              ┌───────────────┐                │
                              │   RabbitMQ    │────────────────┘
                              └───────┬───────┘
                                      │ consume VideoUploaded
                                      ▼
                           ┌─────────────────────┐
                           │  Transcoding.Worker  │
                           │    (no HTTP port)    │
                           └──────────┬──────────┘
                                      │ publish TranscodingCompleted / TranscodingFailed
                                      ▼
                              ┌───────────────┐
                              │   RabbitMQ    │──► VideoManagement.API consumer
                              └───────────────┘
```

### Service Responsibilities

| Service | Responsibilities | Exposed Port |
|---------|-----------------|-------------|
| `Gateway.API` (YARP) | Route `/api/auth/**` → Identity, `/api/videos/**` + `/api/upload/**` → VideoMgmt, `/api/stream/**` → Streaming | `5000` |
| `Identity.API` | Register, login, JWT issuance, user profile | `5001` (internal) |
| `VideoManagement.API` | Upload sessions, chunk handling, video metadata, visibility control | `5002` (internal) |
| `Streaming.API` | HLS manifest serving, segment proxying, access control, playback tracking | `5003` (internal) |
| `Transcoding.Worker` | Consumes `VideoUploadedMessage`, runs FFmpeg pipeline, publishes completion events | none |

### Inter-Service Communication Rules
- **Sync (HTTP)**: Only Client → Gateway → Service. Services never call each other's HTTP APIs.
- **Async (MQ)**: Services integrate exclusively via MassTransit messages defined in `Shared.Contracts`.
- **Data isolation**: Each service has its own DB schema. No DB-level FK constraints across schemas.

---

## 4. Solution Structure

```
DDDProject/
├── src/
│   ├── Services/
│   │   ├── Identity/
│   │   │   ├── Identity.Domain/
│   │   │   ├── Identity.Application/
│   │   │   ├── Identity.Infrastructure/
│   │   │   └── Identity.API/
│   │   │
│   │   ├── VideoManagement/
│   │   │   ├── VideoManagement.Domain/
│   │   │   ├── VideoManagement.Application/
│   │   │   ├── VideoManagement.Infrastructure/
│   │   │   └── VideoManagement.API/
│   │   │
│   │   ├── Streaming/
│   │   │   ├── Streaming.Domain/
│   │   │   ├── Streaming.Application/
│   │   │   ├── Streaming.Infrastructure/
│   │   │   └── Streaming.API/
│   │   │
│   │   └── Transcoding/
│   │       ├── Transcoding.Domain/
│   │       ├── Transcoding.Application/
│   │       ├── Transcoding.Infrastructure/
│   │       └── Transcoding.Worker/
│   │
│   ├── Gateway/
│   │   └── Gateway.API/
│   │
│   └── Shared/
│       ├── Shared.Contracts/
│       └── Shared.BuildingBlocks/
│
├── tests/
│   ├── Identity.Domain.Tests/
│   ├── Identity.Application.Tests/
│   ├── VideoManagement.Domain.Tests/
│   ├── VideoManagement.Application.Tests/
│   ├── Transcoding.Application.Tests/
│   └── Integration.Tests/
│
├── frontend/
│   └── dddproject-ui/
│       ├── src/
│       │   ├── api/
│       │   ├── components/
│       │   ├── hooks/
│       │   ├── pages/
│       │   ├── store/
│       │   └── types/
│       ├── package.json
│       ├── tsconfig.json
│       └── vite.config.ts
│
├── docker/
│   ├── docker-compose.yml
│   ├── docker-compose.dev.yml
│   ├── Dockerfile.identity
│   ├── Dockerfile.videomgmt
│   ├── Dockerfile.streaming
│   ├── Dockerfile.transcoding
│   ├── Dockerfile.gateway
│   └── Dockerfile.frontend
│
└── DDDProject.sln
```

### Clean Architecture Layers (per service)

```
Domain          ← No dependencies. Aggregates, Value Objects, Domain Events, Repository interfaces.
  ▲
Application     ← Depends on Domain only. CQRS handlers, MassTransit consumers/publishers, service interfaces.
  ▲
Infrastructure  ← Depends on Application + Domain. EF Core, blob storage, JWT, FFmpeg implementations.
  ▲
API / Worker    ← Depends on Infrastructure. Controllers, middleware, DI wiring, Program.cs.
```

---

## 5. Shared Libraries

### Shared.BuildingBlocks

```csharp
// Base classes — no external dependencies

public abstract class AggregateRoot<TId>
{
    public TId Id { get; protected set; }
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
    // Equality by Id
}

public abstract class ValueObject
{
    // Equality by all property values (GetEqualityComponents pattern)
}

public interface IDomainEvent { DateTime OccurredAt { get; } }

// MediatR marker wrappers
public interface ICommand<TResult> : IRequest<TResult> { }
public interface IQuery<TResult> : IRequest<TResult> { }
```

### Shared.Contracts (MassTransit message contracts)

```csharp
// All records — immutable, no behavior

public record VideoUploadedMessage(
    Guid VideoId,
    Guid UserId,
    string RawFilePath,
    string FileName,
    DateTime CreatedAt
);

public record TranscodingCompletedMessage(
    Guid VideoId,
    IReadOnlyList<TranscodedVariant> Variants,
    double DurationSeconds
);

public record TranscodedVariant(
    string Resolution,       // "360p" | "480p" | "720p" | "1080p"
    string ManifestPath,     // blob path to index.m3u8
    string SegmentDirectory, // blob path prefix for seg*.ts
    int BitrateKbps,
    int Width,
    int Height
);

public record TranscodingFailedMessage(
    Guid VideoId,
    string Reason
);

public record VideoDeletedMessage(
    Guid VideoId,
    Guid UserId
);
```

---

## 6. Domain Model

### 6.1 Identity Context

**Aggregate: `User`**

```
User
├── Id: UserId (Guid wrapper)
├── Email: Email (RFC-validated, lowercased)
├── Password: HashedPassword (bcrypt hash — raw password never stored or returned)
├── DisplayName: string
├── IsActive: bool
└── CreatedAt: DateTime

Behaviors:
  User.Register(email, password, displayName, IPasswordHashingService) → User
  user.ValidatePassword(raw, IPasswordHashingService) → bool
  user.ChangePassword(currentRaw, newRaw, IPasswordHashingService)

Domain Events:
  UserRegisteredEvent { UserId, Email, DisplayName, OccurredAt }
  UserPasswordChangedEvent { UserId, OccurredAt }
```

### 6.2 VideoManagement Context

**Aggregate: `Video`**

```
Video
├── Id: VideoId
├── OwnerId: Guid (opaque reference to identity.users — no domain type coupling)
├── Title: VideoTitle (non-empty, max 200 chars)
├── Description: string?
├── Status: VideoStatus (Pending | Processing | Ready | Failed | Deleted)
├── Visibility: VideoVisibility (Public | Private)
├── FileSizeBytes: long
├── DurationSeconds: double?
├── RawFilePath: StoragePath
├── Variants: IReadOnlyList<VideoVariant>
└── CreatedAt / PublishedAt / UpdatedAt

Behaviors:
  Video.Create(ownerId, title, description, rawPath, fileSize) → Video
  video.StartTranscoding()           → Status: Pending → Processing
  video.CompleteTranscoding(variants, duration) → Status: Processing → Ready
  video.FailTranscoding(reason)      → Status: Processing → Failed
  video.MakePublic()                 → Visibility: Private → Public
  video.MakePrivate()                → Visibility: Public → Private
  video.UpdateMetadata(title, desc)
  video.Delete()                     → Status: any → Deleted

Domain Events:
  VideoUploadedEvent { VideoId, UserId, RawFilePath, OccurredAt }
  VideoTranscodingStartedEvent { VideoId, OccurredAt }
  VideoTranscodingCompletedEvent { VideoId, Variants, OccurredAt }
  VideoTranscodingFailedEvent { VideoId, Reason, OccurredAt }
  VideoVisibilityChangedEvent { VideoId, Visibility, OccurredAt }
  VideoDeletedEvent { VideoId, UserId, OccurredAt }
```

**Entity: `VideoVariant`** (child of Video aggregate)

```
VideoVariant
├── Id: VideoVariantId
├── VideoId: VideoId
├── Resolution: ResolutionTier (R360p | R480p | R720p | R1080p)
├── BitrateKbps: int
├── Width: int
├── Height: int
├── ManifestPath: StoragePath   → videos/{videoId}/hls/{res}/index.m3u8
└── SegmentDirectory: StoragePath → videos/{videoId}/hls/{res}/
```

**Aggregate: `UploadSession`**

```
UploadSession
├── Id: UploadSessionId
├── UserId: Guid
├── FileName: string
├── TotalFileSizeBytes: long
├── TotalChunks: int
├── ChunkSizeBytes: int (default 5 MB)
├── ReceivedChunks: List<int>  (chunk indices received, stored as JSON)
├── Status: UploadSessionStatus (Active | Completed | Aborted)
├── TempDirectory: StoragePath  → uploads/{sessionId}/
├── CreatedAt: DateTime
└── ExpiresAt: DateTime  (24 hours after creation)

Behaviors:
  UploadSession.Create(userId, fileName, fileSize, totalChunks, chunkSize) → UploadSession
  session.RecordChunkReceived(chunkIndex)
  session.IsComplete → bool (ReceivedChunks.Count == TotalChunks)
  session.Complete()
  session.Abort()
```

### 6.3 Transcoding Context

**Aggregate: `TranscodingJob`**

```
TranscodingJob
├── Id: TranscodingJobId
├── VideoId: Guid
├── Status: TranscodingJobStatus (Queued | Running | Completed | Failed)
├── ErrorMessage: string?
├── StartedAt: DateTime?
├── CompletedAt: DateTime?
└── CreatedAt: DateTime

Behaviors:
  TranscodingJob.Create(videoId) → TranscodingJob
  job.Start()
  job.Complete()
  job.Fail(reason)
```

### 6.4 Streaming Context

**Aggregate: `PlaybackSession`**

```
PlaybackSession
├── Id: PlaybackSessionId
├── VideoId: Guid
├── ViewerId: Guid?  (null = anonymous)
├── IpAddress: string?
├── StartedAt: DateTime
├── EndedAt: DateTime?
└── BytesServed: long

Behaviors:
  PlaybackSession.Start(videoId, viewerId?, ipAddress?) → PlaybackSession
  session.RecordBytesServed(bytes)
  session.End()
```

---

## 7. CQRS Commands & Queries

### Identity.Application

| Type | Name | Input | Output |
|------|------|-------|--------|
| Command | `RegisterUserCommand` | email, password, displayName | `Guid` (userId) |
| Command | `LoginUserCommand` | email, password | `JwtTokenDto` { accessToken, expiresAt } |
| Query | `GetUserProfileQuery` | userId | `UserProfileDto` { id, email, displayName, createdAt } |

### VideoManagement.Application

| Type | Name | Input | Output |
|------|------|-------|--------|
| Command | `InitiateUploadCommand` | userId, fileName, fileSizeBytes, totalChunks, title, description? | `InitiateUploadDto` { sessionId, chunkSizeBytes } |
| Command | `UploadChunkCommand` | sessionId, userId, chunkIndex, data (Stream) | `ChunkAckDto` { receivedCount, totalChunks } |
| Command | `FinalizeUploadCommand` | sessionId, userId | `Guid` (videoId) |
| Command | `AbortUploadCommand` | sessionId, userId | `Unit` |
| Command | `ChangeVideoVisibilityCommand` | videoId, userId, visibility | `Unit` |
| Command | `UpdateVideoMetadataCommand` | videoId, userId, title, description? | `Unit` |
| Command | `DeleteVideoCommand` | videoId, userId | `Unit` |
| Query | `GetVideoByIdQuery` | videoId, requestingUserId? | `VideoDetailDto` |
| Query | `GetVideosByOwnerQuery` | userId, page, pageSize | `PagedResult<VideoSummaryDto>` |
| Query | `GetPublicVideosQuery` | page, pageSize, searchTerm? | `PagedResult<VideoSummaryDto>` |
| MQ Consumer | `TranscodingCompletedConsumer` | `TranscodingCompletedMessage` | — |
| MQ Consumer | `TranscodingFailedConsumer` | `TranscodingFailedMessage` | — |

### Transcoding.Application

| Type | Name | Input | Output |
|------|------|-------|--------|
| MQ Consumer | `VideoUploadedConsumer` | `VideoUploadedMessage` | publishes `TranscodingCompleted` or `TranscodingFailed` |
| Command | `StartTranscodingCommand` | videoId, rawFilePath, fileName | `Guid` (jobId) |
| MQ Consumer | `VideoDeletedConsumer` | `VideoDeletedMessage` | cancels in-progress job if any |

### Streaming.Application

| Type | Name | Input | Output |
|------|------|-------|--------|
| Query | `GetMasterManifestQuery` | videoId, requestingUserId? | `string` (m3u8 content) |
| Query | `GetVariantManifestQuery` | videoId, resolution, requestingUserId? | `string` (m3u8 content) |
| Query | `GetSegmentQuery` | videoId, resolution, segmentFile, requestingUserId? | `Stream` |
| Command | `StartPlaybackSessionCommand` | videoId, viewerId?, ipAddress? | `Guid` (sessionId) |
| Command | `EndPlaybackSessionCommand` | sessionId, bytesServed | `Unit` |
| MQ Consumer | `VideoDeletedConsumer` | `VideoDeletedMessage` | invalidates any cached manifests |

---

## 8. API Endpoints

All endpoints are accessed via **API Gateway at port 5000**. The gateway strips no path prefixes — it proxies as-is.

### Auth — routes to Identity.API

| Method | Path | Auth | Request Body | Response |
|--------|------|------|-------------|----------|
| `POST` | `/api/auth/register` | None | `{ email, password, displayName }` | `201 { userId }` |
| `POST` | `/api/auth/login` | None | `{ email, password }` | `200 { accessToken, expiresAt }` |
| `GET` | `/api/auth/me` | JWT Bearer | — | `200 UserProfileDto` |

### Videos — routes to VideoManagement.API

| Method | Path | Auth | Request / Params | Response |
|--------|------|------|-----------------|----------|
| `GET` | `/api/videos` | None | `?page=1&pageSize=20&search=` | `200 PagedResult<VideoSummaryDto>` |
| `GET` | `/api/videos/my` | JWT | `?page=1&pageSize=20` | `200 PagedResult<VideoSummaryDto>` |
| `GET` | `/api/videos/{videoId}` | Optional JWT | — | `200 VideoDetailDto` |
| `PATCH` | `/api/videos/{videoId}/visibility` | JWT (owner) | `{ visibility: "Public"\|"Private" }` | `204` |
| `PATCH` | `/api/videos/{videoId}/metadata` | JWT (owner) | `{ title, description? }` | `204` |
| `DELETE` | `/api/videos/{videoId}` | JWT (owner) | — | `204` |

### Upload — routes to VideoManagement.API

| Method | Path | Auth | Request Body | Response |
|--------|------|------|-------------|----------|
| `POST` | `/api/upload/initiate` | JWT | `{ fileName, fileSizeBytes, totalChunks, title, description? }` | `200 { sessionId, chunkSizeBytes }` |
| `PUT` | `/api/upload/{sessionId}/chunk/{chunkIndex}` | JWT | Raw binary body (chunk bytes) | `200 { receivedCount, totalChunks }` |
| `POST` | `/api/upload/{sessionId}/finalize` | JWT | — | `202 { videoId }` |
| `DELETE` | `/api/upload/{sessionId}` | JWT | — | `204` |

### Streaming — routes to Streaming.API

| Method | Path | Auth | Response |
|--------|------|------|----------|
| `GET` | `/api/stream/{videoId}/master.m3u8` | Optional JWT | `200 text/plain` HLS master manifest |
| `GET` | `/api/stream/{videoId}/{resolution}/index.m3u8` | Optional JWT | `200 text/plain` HLS variant manifest |
| `GET` | `/api/stream/{videoId}/{resolution}/{segmentFile}` | Optional JWT | `200 video/mp2t` MPEG-TS segment bytes |

**Access control for streaming:**
- Public videos: accessible without authentication
- Private videos: require JWT; token's sub claim must match `videos.owner_id`
- Unauthorized access to private video returns `403 Forbidden`

### DTO Shapes

```typescript
// VideoSummaryDto
{ id, title, ownerDisplayName, durationSeconds, thumbnailUrl, visibility, status, createdAt }

// VideoDetailDto
{ id, title, description, ownerDisplayName, durationSeconds, visibility, status,
  variants: [{ resolution, bitrateKbps, width, height }], createdAt }

// PagedResult<T>
{ items: T[], page, pageSize, totalCount, totalPages }
```

---

## 9. Database Schema (SQL Server)

One SQL Server instance. Schemas are prefixed to match bounded contexts. No FK constraints cross schema boundaries.

### `identity` schema

```sql
CREATE SCHEMA identity;
GO

CREATE TABLE identity.users (
    id              UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    email           NVARCHAR(320)       NOT NULL,
    display_name    NVARCHAR(100)       NOT NULL,
    password_hash   NVARCHAR(72)        NOT NULL,
    is_active       BIT                 NOT NULL DEFAULT 1,
    created_at      DATETIMEOFFSET(7)   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    updated_at      DATETIMEOFFSET(7)   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT UQ_users_email UNIQUE (email)
);
CREATE INDEX IX_users_email ON identity.users (email);
```

### `video_mgmt` schema

```sql
CREATE SCHEMA video_mgmt;
GO

CREATE TABLE video_mgmt.videos (
    id                  UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    owner_id            UNIQUEIDENTIFIER    NOT NULL,           -- no FK to identity.users
    title               NVARCHAR(200)       NOT NULL,
    description         NVARCHAR(MAX),
    status              NVARCHAR(20)        NOT NULL DEFAULT 'Pending',
    visibility          NVARCHAR(10)        NOT NULL DEFAULT 'Private',
    file_size_bytes     BIGINT              NOT NULL,
    duration_seconds    DECIMAL(10,3),
    raw_file_path       NVARCHAR(1000),
    transcoding_error   NVARCHAR(MAX),
    created_at          DATETIMEOFFSET(7)   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    updated_at          DATETIMEOFFSET(7)   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    published_at        DATETIMEOFFSET(7)
);
CREATE INDEX IX_videos_owner_id           ON video_mgmt.videos (owner_id);
CREATE INDEX IX_videos_status_visibility  ON video_mgmt.videos (status, visibility);

CREATE TABLE video_mgmt.video_variants (
    id                  UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    video_id            UNIQUEIDENTIFIER    NOT NULL REFERENCES video_mgmt.videos(id) ON DELETE CASCADE,
    resolution          NVARCHAR(10)        NOT NULL,   -- '360p' | '480p' | '720p' | '1080p'
    bitrate_kbps        INT                 NOT NULL,
    width               INT                 NOT NULL,
    height              INT                 NOT NULL,
    manifest_path       NVARCHAR(1000)      NOT NULL,
    segment_directory   NVARCHAR(1000)      NOT NULL,
    created_at          DATETIMEOFFSET(7)   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT UQ_video_variants_video_resolution UNIQUE (video_id, resolution)
);
CREATE INDEX IX_video_variants_video_id ON video_mgmt.video_variants (video_id);

CREATE TABLE video_mgmt.upload_sessions (
    id                  UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    user_id             UNIQUEIDENTIFIER    NOT NULL,
    file_name           NVARCHAR(500)       NOT NULL,
    total_file_size     BIGINT              NOT NULL,
    total_chunks        INT                 NOT NULL,
    chunk_size_bytes    INT                 NOT NULL,
    received_chunks     NVARCHAR(MAX)       NOT NULL DEFAULT '[]',  -- JSON array of int
    status              NVARCHAR(20)        NOT NULL DEFAULT 'Active',
    temp_directory      NVARCHAR(1000)      NOT NULL,
    created_at          DATETIMEOFFSET(7)   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    expires_at          DATETIMEOFFSET(7)   NOT NULL
);
CREATE INDEX IX_upload_sessions_user_id    ON video_mgmt.upload_sessions (user_id);
CREATE INDEX IX_upload_sessions_expires_at ON video_mgmt.upload_sessions (expires_at);
```

### `streaming` schema

```sql
CREATE SCHEMA streaming;
GO

CREATE TABLE streaming.playback_sessions (
    id              UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    video_id        UNIQUEIDENTIFIER    NOT NULL,   -- no FK to video_mgmt.videos
    viewer_id       UNIQUEIDENTIFIER,               -- null = anonymous viewer
    ip_address      NVARCHAR(45),
    started_at      DATETIMEOFFSET(7)   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    ended_at        DATETIMEOFFSET(7),
    bytes_served    BIGINT              NOT NULL DEFAULT 0
);
CREATE INDEX IX_playback_sessions_video_id  ON streaming.playback_sessions (video_id);
CREATE INDEX IX_playback_sessions_viewer_id ON streaming.playback_sessions (viewer_id);
```

### `transcoding` schema

```sql
CREATE SCHEMA transcoding;
GO

CREATE TABLE transcoding.transcoding_jobs (
    id              UNIQUEIDENTIFIER    NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    video_id        UNIQUEIDENTIFIER    NOT NULL,
    status          NVARCHAR(20)        NOT NULL DEFAULT 'Queued',
    error_message   NVARCHAR(MAX),
    started_at      DATETIMEOFFSET(7),
    completed_at    DATETIMEOFFSET(7),
    created_at      DATETIMEOFFSET(7)   NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
CREATE INDEX IX_transcoding_jobs_video_id ON transcoding.transcoding_jobs (video_id);
```

---

## 10. Message Queue — RabbitMQ + MassTransit

### Message Flow

| Message | Publisher | Consumer(s) | Queue |
|---------|-----------|------------|-------|
| `VideoUploadedMessage` | VideoManagement.API | Transcoding.Worker | `video-uploaded` |
| `TranscodingCompletedMessage` | Transcoding.Worker | VideoManagement.API | `transcoding-completed` |
| `TranscodingFailedMessage` | Transcoding.Worker | VideoManagement.API | `transcoding-failed` |
| `VideoDeletedMessage` | VideoManagement.API | Transcoding.Worker, Streaming.API | `video-deleted` |

### MassTransit Configuration (per service)

```csharp
services.AddMassTransit(x =>
{
    // Register consumers (service-specific)
    x.AddConsumer<VideoUploadedConsumer>();  // Transcoding.Worker only

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.UseMessageRetry(r => r.Exponential(3,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(5)));

        cfg.ConfigureEndpoints(ctx);
    });
});
```

Dead-letter behaviour: Messages exhausting all retry attempts are automatically moved to `{queue}-error` exchange by MassTransit's default error transport. No additional configuration required.

### Transcoding Flow Detail

```
1. POST /api/upload/{sessionId}/finalize
   → FinalizeUploadCommandHandler assembles raw file
   → Creates Video aggregate (Status=Pending)
   → Dispatches VideoUploadedMessage via IPublishEndpoint

2. Transcoding.Worker: VideoUploadedConsumer.Consume(context)
   → Creates TranscodingJob (Status=Running)
   → Calls FFmpegTranscodingService.TranscodeAsync(videoId, rawFilePath)
   → On success: publishes TranscodingCompletedMessage
   → On exception: publishes TranscodingFailedMessage
   → MassTransit retry handles transient FFmpeg failures (up to 3 attempts)

3. VideoManagement.API: TranscodingCompletedConsumer.Consume(context)
   → Loads Video aggregate from repository
   → Calls video.CompleteTranscoding(variants, duration)
   → Saves VideoVariant entities to DB
   → Status transitions to Ready

4. VideoManagement.API: TranscodingFailedConsumer.Consume(context)
   → Loads Video aggregate
   → Calls video.FailTranscoding(reason)
   → Status transitions to Failed
```

---

## 11. Blob Storage Layout

```
{blob-root}/
│
├── uploads/
│   └── {uploadSessionId}/
│       ├── chunk_0000          ← raw binary, no extension
│       ├── chunk_0001
│       └── chunk_{N:D4}
│
├── raw/
│   └── {videoId}/
│       └── original.mp4        ← assembled file after finalize
│
└── videos/
    └── {videoId}/
        └── hls/
            ├── master.m3u8
            ├── 360p/
            │   ├── index.m3u8
            │   ├── seg000.ts
            │   └── seg001.ts ...
            ├── 480p/
            │   ├── index.m3u8
            │   └── seg*.ts
            ├── 720p/
            │   ├── index.m3u8
            │   └── seg*.ts
            └── 1080p/
                ├── index.m3u8
                └── seg*.ts
```

### IBlobStorageService Interface

```csharp
public interface IBlobStorageService
{
    Task UploadAsync(string path, Stream data, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string path, CancellationToken ct = default);
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task DeleteDirectoryAsync(string pathPrefix, CancellationToken ct = default);
    Task<IEnumerable<string>> ListPathsAsync(string pathPrefix, CancellationToken ct = default);
    // Returns null if implementation does not support signed URLs (local FS)
    Task<Uri?> GetSignedDownloadUriAsync(string path, TimeSpan expiry, CancellationToken ct = default);
}
```

**Local file system implementation**: `LocalBlobStorageService` maps paths to a configurable root directory (e.g., `C:\blob-storage` or `/var/blob`). Methods translate blob path strings to `System.IO.File` / `Directory` operations. `GetSignedDownloadUriAsync` returns `null`.

---

## 12. Video Processing Pipeline

### Complete End-to-End Flow

```
[CLIENT]
  │
  ├─ POST /api/upload/initiate
  │   body: { fileName, fileSizeBytes, totalChunks, title, description }
  │   → InitiateUploadCommand
  │       → UploadSession.Create(...)
  │       → Creates blob directory: uploads/{sessionId}/
  │       → Saves session to DB
  │   ← { sessionId, chunkSizeBytes: 5_242_880 }
  │
  ├─ PUT /api/upload/{sessionId}/chunk/{0..N} [up to 3 concurrent from client]
  │   body: raw binary (≤ 5 MB)
  │   → UploadChunkCommand
  │       → Validates session Active + not expired + chunkIndex in range
  │       → Writes to blob: uploads/{sessionId}/chunk_{index:D4}
  │       → session.RecordChunkReceived(index)
  │       → Updates DB
  │   ← { receivedCount, totalChunks }
  │
  └─ POST /api/upload/{sessionId}/finalize
      → FinalizeUploadCommand
          → Validates session.IsComplete
          → Reads chunks 0..N from blob in order, concatenates to Stream
          → Writes Stream to blob: raw/{videoId}/original.mp4
          → Deletes blob directory: uploads/{sessionId}/
          → Video.Create(ownerId, title, description, rawPath, fileSize) → Status=Pending
          → Saves Video to DB
          → session.Complete() → Status=Completed
          → Publishes VideoUploadedMessage to RabbitMQ
      ← 202 { videoId }

[RABBITMQ] → Transcoding.Worker: VideoUploadedConsumer

  TranscodingJob.Create(videoId) → Status=Queued
  job.Start() → Status=Running

  FFmpegTranscodingService.TranscodeAsync(videoId, rawFilePath):
    1. Run ffprobe to get source width, height, duration
    2. For each tier in [360p, 480p, 720p, 1080p]:
       IF source_height < tier.height → skip (no upscale)
       ELSE:
         Build FFmpeg args:
           -i {rawFilePath}
           -vf scale={tier.width}:{tier.height}
           -c:v libx264 -preset fast -crf 23 -b:v {tier.videoBitrateKbps}k
           -c:a aac -b:a {tier.audioBitrateKbps}k
           -hls_time 6
           -hls_playlist_type vod
           -hls_segment_filename {tmpDir}/{tier.name}/seg%03d.ts
           {tmpDir}/{tier.name}/index.m3u8
         Start System.Diagnostics.Process(ffmpeg, args)
         Await exit → check ExitCode == 0
         Upload segments from tmpDir to blob: videos/{videoId}/hls/{tier}/seg*.ts
         Upload manifest: videos/{videoId}/hls/{tier}/index.m3u8
         Record VideoVariant for this tier
    3. Generate master.m3u8 content (see format below)
    4. Upload master.m3u8 to blob: videos/{videoId}/hls/master.m3u8
    5. Delete tmpDir

  On success:
    job.Complete()
    Publish TranscodingCompletedMessage { VideoId, Variants, DurationSeconds }

  On failure:
    job.Fail(errorMessage)
    Publish TranscodingFailedMessage { VideoId, Reason }

[RABBITMQ] → VideoManagement.API: TranscodingCompletedConsumer
  video.CompleteTranscoding(variants, duration) → Status=Ready
  Saves VideoVariant records
  Sets published_at if Visibility=Public

[RABBITMQ] → VideoManagement.API: TranscodingFailedConsumer
  video.FailTranscoding(reason) → Status=Failed
```

### FFmpeg Resolution / Bitrate Profile

| Tier | Width | Height | Video Bitrate | Audio Bitrate | Segment (~6s) |
|------|-------|--------|--------------|--------------|--------------|
| 360p | 640 | 360 | 800 kbps | 96 kbps | ~600 KB |
| 480p | 854 | 480 | 1400 kbps | 128 kbps | ~1.1 MB |
| 720p | 1280 | 720 | 2800 kbps | 128 kbps | ~2.2 MB |
| 1080p | 1920 | 1080 | 5000 kbps | 192 kbps | ~3.9 MB |

### HLS Master Manifest Format

```
#EXTM3U
#EXT-X-VERSION:3

#EXT-X-STREAM-INF:BANDWIDTH=896000,RESOLUTION=640x360
/api/stream/{videoId}/360p/index.m3u8

#EXT-X-STREAM-INF:BANDWIDTH=1528000,RESOLUTION=854x480
/api/stream/{videoId}/480p/index.m3u8

#EXT-X-STREAM-INF:BANDWIDTH=2928000,RESOLUTION=1280x720
/api/stream/{videoId}/720p/index.m3u8

#EXT-X-STREAM-INF:BANDWIDTH=5192000,RESOLUTION=1920x1080
/api/stream/{videoId}/1080p/index.m3u8
```

Manifest URLs point back through the API so that access control is enforced on every request (private video protection). Bandwidth values include both video and audio bitrates.

---

## 13. Frontend Structure

### Page Routes (React Router v6)

| Route | Component | Auth |
|-------|-----------|------|
| `/` | `HomePage` | None |
| `/login` | `LoginPage` | None (redirect to `/` if already logged in) |
| `/register` | `RegisterPage` | None |
| `/upload` | `UploadPage` | Required (ProtectedRoute) |
| `/my-videos` | `MyVideosPage` | Required (ProtectedRoute) |
| `/watch/:videoId` | `VideoWatchPage` | None (private videos handled by API) |

### Component Tree

```
App
├── Navbar
└── Routes
    ├── HomePage
    │   └── VideoGrid → VideoCard (×N)
    ├── LoginPage
    │   └── LoginForm
    ├── RegisterPage
    │   └── RegisterForm
    ├── UploadPage (ProtectedRoute)
    │   ├── UploadForm  (file picker, title, description inputs)
    │   ├── ChunkUploader  (drives upload state machine)
    │   └── UploadProgressBar
    ├── MyVideosPage (ProtectedRoute)
    │   └── VideoGrid
    │       └── VideoCard (with owner controls)
    │           └── VisibilityToggle
    └── VideoWatchPage
        ├── VideoPlayer  (HLS.js)
        └── VideoDetails
```

### Key Components & Hooks

**`VideoPlayer.tsx`**
```typescript
// Props: { videoId: string }
// - Constructs masterManifestUrl: /api/stream/{videoId}/master.m3u8
// - Creates Hls instance; attaches to <video> ref
// - if (!Hls.isSupported()) falls back to native src= (Safari)
// - Exposes quality level list for optional manual override UI
// - Cleanup: hls.destroy() on unmount
```

**`useChunkUpload.ts`**
```typescript
// Returns: { start, pause, resume, progress, status, videoId, error }
// Flow:
//   1. POST /api/upload/initiate → { sessionId, chunkSizeBytes }
//   2. file.slice(start, end) → Blob → PUT chunk (fetch, raw binary)
//      - max 3 concurrent chunks via Promise.all batching
//   3. POST /api/upload/{sessionId}/finalize → { videoId }
// Resume: on retry, query remaining chunks and skip already-uploaded indices
```

**`VisibilityToggle.tsx`**
```typescript
// Props: { videoId: string, currentVisibility: 'Public' | 'Private' }
// - Optimistic UI: update local state immediately
// - PATCH /api/videos/{videoId}/visibility
// - On error: rollback local state + show toast
```

### State Management

```typescript
// authSlice (Redux Toolkit)
{ user: UserProfileDto | null, token: string | null, status: 'idle' | 'loading' | 'error' }

// uploadSlice (Redux Toolkit)
{ sessionId: string | null, progress: number, status: UploadStatus, videoId?: string, error?: string }

// All GET queries: React Query (TanStack)
// - useQuery(['videos', 'public', page], fetchPublicVideos)
// - useQuery(['videos', 'my', page], fetchMyVideos)
// - useQuery(['video', videoId], fetchVideoById)
```

### API Client

```typescript
// axiosClient.ts
// Axios instance with:
//   baseURL: import.meta.env.VITE_API_BASE_URL (defaults to http://localhost:5000)
//   request interceptor: adds Authorization: Bearer {token} from Redux store
//   response interceptor: on 401 → dispatch(logout()) + navigate('/login')

// uploadApi.ts uses native fetch (not Axios) for chunk PUT requests
// to avoid Axios overhead and to use ReadableStream for progress tracking
```

---

## 14. Docker Setup

### docker-compose.yml

```yaml
version: '3.9'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "StrongPassword123!"
      ACCEPT_EULA: "Y"
    ports: ["1433:1433"]
    volumes: ["sqlserver-data:/var/opt/mssql"]
    networks: [dddproject-network]

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"    # AMQP
      - "15672:15672"  # Management UI
    volumes: ["rabbitmq-data:/var/lib/rabbitmq"]
    networks: [dddproject-network]

  gateway:
    build:
      context: .
      dockerfile: docker/Dockerfile.gateway
    ports: ["5000:80"]
    depends_on: [identity-api, videomgmt-api, streaming-api]
    networks: [dddproject-network]

  identity-api:
    build:
      context: .
      dockerfile: docker/Dockerfile.identity
    environment:
      ConnectionStrings__Default: "Server=sqlserver;Database=DDDProject;User Id=sa;Password=StrongPassword123!;TrustServerCertificate=True"
      Jwt__Secret: "your-256-bit-secret-here"
      Jwt__ExpiryMinutes: "15"
    depends_on: [sqlserver]
    networks: [dddproject-network]

  videomgmt-api:
    build:
      context: .
      dockerfile: docker/Dockerfile.videomgmt
    environment:
      ConnectionStrings__Default: "Server=sqlserver;..."
      RabbitMq__Host: "rabbitmq"
      BlobStorage__Root: "/blob"
    volumes: ["blob-data:/blob"]
    depends_on: [sqlserver, rabbitmq]
    networks: [dddproject-network]

  streaming-api:
    build:
      context: .
      dockerfile: docker/Dockerfile.streaming
    environment:
      ConnectionStrings__Default: "Server=sqlserver;..."
      BlobStorage__Root: "/blob"
    volumes: ["blob-data:/blob"]
    depends_on: [sqlserver]
    networks: [dddproject-network]

  transcoding-worker:
    build:
      context: .
      dockerfile: docker/Dockerfile.transcoding
    environment:
      ConnectionStrings__Default: "Server=sqlserver;..."
      RabbitMq__Host: "rabbitmq"
      BlobStorage__Root: "/blob"
      FFmpeg__Path: "/usr/bin/ffmpeg"
    volumes: ["blob-data:/blob"]
    depends_on: [sqlserver, rabbitmq]
    networks: [dddproject-network]

  frontend:
    build:
      context: frontend/dddproject-ui
      dockerfile: ../../docker/Dockerfile.frontend
    ports: ["3000:80"]
    depends_on: [gateway]
    networks: [dddproject-network]

volumes:
  sqlserver-data:
  rabbitmq-data:
  blob-data:

networks:
  dddproject-network:
    driver: bridge
```

The `Transcoding.Worker` Dockerfile must include FFmpeg:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y ffmpeg
COPY ...
```

### docker-compose.dev.yml (overrides for hot-reload)

Adds volume mounts for `dotnet watch` on API services and Vite HMR on the frontend. Services are run with `ASPNETCORE_ENVIRONMENT=Development`.

---

## 15. Key Technical Decisions

| # | Decision | Choice | Rationale |
|---|----------|--------|-----------|
| 1 | Service decomposition | 4 services + gateway | Each bounded context is independently deployable; Transcoding.Worker scales independently of the API services |
| 2 | Message queue | RabbitMQ + MassTransit | MassTransit provides declarative retries, dead-letter, and consumer wiring; swappable broker (Azure Service Bus, ActiveMQ) with config change only |
| 3 | API gateway | YARP | Native .NET, config-driven, no separate process needed in dev, easy JWT passthrough |
| 4 | Chunked upload | 5 MB chunks, up to 3 parallel | Resumable (ReceivedChunks persisted in DB); large files never time out; client uses `File.slice()` |
| 5 | Video format | HLS-only | HLS.js covers all non-Safari browsers; Safari uses native HLS; no dual-format complexity |
| 6 | Segment duration | 6 seconds | Apple default; balances ABR switching speed vs. file count and seek precision |
| 7 | Segment serving | Proxy through Streaming.API | Enforces per-request access control on private videos; production path: 307 redirect to signed blob URL |
| 8 | Transcoding parallelism | Sequential per resolution tier | Predictable CPU usage; max concurrent jobs controlled via semaphore (`SemaphoreSlim`) in worker; simpler error handling |
| 9 | Resolution upscaling | Never upscale | Detect source height via ffprobe before transcoding; skip tiers where source < target |
| 10 | DB cross-context FKs | None across schemas | Preserves service independence; future DB-per-service extraction requires only connection string change |
| 11 | Raw file retention | Keep after transcoding | Allows re-transcoding; optional cleanup job deletes `raw/` after video status = Ready |
| 12 | Auth tokens | 15-min JWT access token only | Simple for v1; `IJwtTokenService` interface allows adding refresh tokens in v2 |
| 13 | Blob abstraction | `IBlobStorageService` | Local FS in v1; swap to MinIO (S3-compatible) or Azure Blob by changing only the Infrastructure registration |
| 14 | received_chunks storage | JSON array in NVARCHAR(MAX) | Sufficient for files up to ~50 GB at 5 MB/chunk; avoids junction table |

---

## 16. NuGet Package List

### Per-Service Common

| Package | Purpose |
|---------|---------|
| `MediatR` | In-process CQRS dispatch |
| `FluentValidation.DependencyInjectionExtensions` | Command/query validation pipeline |
| `Microsoft.EntityFrameworkCore.SqlServer` | EF Core SQL Server provider |
| `MassTransit.RabbitMQ` | RabbitMQ + MassTransit integration |

### Identity.Infrastructure

| Package | Purpose |
|---------|---------|
| `BCrypt.Net-Next` | Password hashing |
| `System.IdentityModel.Tokens.Jwt` | JWT creation and validation |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT middleware |

### Gateway.API

| Package | Purpose |
|---------|---------|
| `Yarp.ReverseProxy` | YARP reverse proxy |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Optional gateway-level JWT validation |

### Transcoding.Infrastructure

| Package | Purpose |
|---------|---------|
| *(FFmpeg invoked as OS process via `System.Diagnostics.Process`)* | — |

### Frontend

| Package | Purpose |
|---------|---------|
| `hls.js` | HLS adaptive bitrate player |
| `axios` | HTTP client with interceptors |
| `@reduxjs/toolkit` + `react-redux` | Auth and upload state |
| `@tanstack/react-query` | Server state, caching, refetch |
| `react-router-dom` | Client-side routing |

---

## 17. Verification Checklist

Before beginning implementation, verify this spec is internally consistent:

- [ ] Every MassTransit message in `Shared.Contracts` has exactly one publisher and at least one consumer listed in §10
- [ ] Every CQRS command/query in §7 has a corresponding API endpoint in §8
- [ ] Every DB table in §9 has a corresponding domain aggregate or entity in §6
- [ ] Blob storage paths referenced in the pipeline (§12) match the layout diagram (§11)
- [ ] HLS manifest URL pattern (`/api/stream/{videoId}/{res}/index.m3u8`) matches the Streaming.API route in §8
- [ ] All docker-compose services in §14 have a corresponding service in §3
- [ ] `Shared.Contracts` message field names match what publishers set and consumers read
