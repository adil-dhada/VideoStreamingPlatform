# Shared Building Blocks & Contracts

## Role
This directory contains generic standard libraries shared natively across all microservices (published as NuGet or project-references).

## Sub-Projects
- `Shared.BuildingBlocks`: Base Clean Architecture domain primitives like `AggregateRoot`, `Entity`, `IDomainEvent`, and standard abstract services like `IBlobStorageService`. Note: This layer ensures robust DDD constraints.
- `Shared.Contracts`: Integration event objects (e.g. `VideoUploadedMessage`, `TranscodingCompletedMessage`) pushed across the MassTransit/RabbitMQ queue.

## Impact on Solution
Provides a unified language and standardizes the domain models across the disconnected decoupled Bounded Contexts. Updating these packages carries heavy downstream implications and should be done conservatively.
