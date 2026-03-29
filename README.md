# Video Streaming Platform

A microservice-based video streaming platform built from scratch with .NET 9.

## Overview
This platform is logically partitioned into microservices:
1. **Gateway**: YARP-based reverse proxy managing traffic routing.
2. **Identity**: JWT authentication and user profile management.
3. **Video Management**: Resumable chunked file uploads and video metadata persistence.
4. **Transcoding Worker**: A background worker that kicks off FFmpeg to generate HLS segments.
5. **Streaming**: High-performance segment serving using byte-range and proxy forwarding.

## Setup Instructions

### Traditional Local Development
To run this solution locally, you must have the following dependencies running:
- **SQL Server**: Ensure it's listening locally on `1433`.
- **RabbitMQ**: Ensure it's listening locally on `5672` (default guest credentials).

1. Ensure the Solution Restores:
   ```bash
   dotnet restore VideoStreamingPlatform.sln
   ```
2. Set up SQL server (You might need EF migrations, or ensuring the DB exists. E.g. `dotnet ef database update --project src/Services/Identity/Identity.Infrastructure`).
3. Run the microservices:
   ```bash
   dotnet run --project src/Gateway/Gateway.API
   dotnet run --project src/Services/Identity/Identity.API
   dotnet run --project src/Services/VideoManagement/VideoManagement.API
   dotnet run --project src/Services/Transcoding/Transcoding.Worker
   ```

### Docker Compose
A `docker-compose.yml` is being provisioned or provided to spin up SQL Server, RabbitMQ, and the microservices simultaneously.
Once provided:
```bash
docker-compose up -d --build
```
This will mount temp volumes, provision local development environments, and start all applications behind Gateway on `localhost:5000`.

## Architecture
- Clean Architecture (Domain, Application, Infrastructure, API).
- CQRS using MediatR.
- Message Broker using MassTransit + RabbitMQ.
