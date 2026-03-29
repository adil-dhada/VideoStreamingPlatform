# Streaming Microservice

## Role
Proxies Multi-bitrate HLS playlists and raw .ts chunk objects natively from `LocalBlobStorageService`, bypassing load on other Application contexts. 

## Structure
A minimal ASP.NET Core API with heavy controller focus `StreamController.cs`. 

## Impact on Solution
Fastest, lightest weight service. Requires minimal dependencies (no EF Core, no rabbitMQ). Responsible for pushing binary chunks to thousands of concurrent users rapidly using raw async Streams and ensuring private videos are not accessed by illegitimate parties (verified by pinging the `VideoManagement` internal API endpoint).
