# Transcoding Worker

## Role
An asynchronous BackgroundService worker node strictly decoupled from ingress APIs.

## Sub-Projects
Based on Clean Architecture:
- `Transcoding.Domain`: Defines asynchronous `TranscodingJob` statuses and progression states.
- `Transcoding.Application`: Contains MassTransit message consumers like `VideoUploadedConsumer`.
- `Transcoding.Infrastructure`: Defines the `FFmpegTranscodingService` which spawns isolated operating-system native sub-processes securely.
- `Transcoding.Worker`: Hosted execution instance running forever parsing AMQP packets from RabbitMQ.

## Impact on Solution
When `Video Management` emits a signal that a raw video finished uploading, this independent Worker detects it immediately and initiates expensive IO-bound subprocessing to convert the file into Multi-bitrate HTTP Live Streaming (HLS) formats seamlessly without blocking API ingestion endpoints.
