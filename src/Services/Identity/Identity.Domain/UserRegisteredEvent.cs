using System;
using Shared.BuildingBlocks;

namespace Identity.Domain;

public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime OccurredAt
) : IDomainEvent;
