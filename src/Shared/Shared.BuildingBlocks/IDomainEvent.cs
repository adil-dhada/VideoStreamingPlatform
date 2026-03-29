using System;

namespace Shared.BuildingBlocks;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
