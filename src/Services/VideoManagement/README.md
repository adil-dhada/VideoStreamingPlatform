# Video Management Microservice

## Role
Responsible for the multi-stage ingestion of large binary payloads using a resumable chunked workflow, and maintaining user-specific video libraries.

## Sub-Projects
Based on Clean Architecture:
- `VideoManagement.Domain`: Models tracking `UploadSession`, chunk offsets, and resulting `Video` and `VideoVariant` objects.
- `VideoManagement.Application`: The chunk assembly logic and metadata update handlers (`InitiateUploadCommand`, `UploadChunkCommand`, `FinalizeUploadCommand`).
- `VideoManagement.Infrastructure`: EF Core DbContext for video metadata, and interactions with `LocalBlobStorageService`.
- `VideoManagement.API`: The ingress for chunk binary byte-streams, exposing endpoints used actively by browsers performing `File.slice()` operations.

## Impact on Solution
It guarantees that files gigabytes large do not bottleneck a REST call or crash local memory. It parses chunks iteratively before re-building a raw payload and dropping a mass transit event onto the `VideoUploadedMessage` bus, ensuring smooth scaling and upload reliability.
