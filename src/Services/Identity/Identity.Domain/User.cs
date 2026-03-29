using System;
using Shared.BuildingBlocks;

namespace Identity.Domain;

public class User : AggregateRoot<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private User() { } // ORM requirement

    private User(Guid id, string email, string passwordHash, string displayName)
    {
        Id = id;
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        DisplayName = displayName;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static User Register(string email, string rawPassword, string displayName, IPasswordHashingService passwordHasher)
    {
        var id = Guid.NewGuid();
        var hash = passwordHasher.HashPassword(rawPassword);
        var user = new User(id, email, hash, displayName);
        
        user.RaiseDomainEvent(new UserRegisteredEvent(id, user.Email, user.DisplayName, DateTime.UtcNow));
        return user;
    }

    public bool ValidatePassword(string rawPassword, IPasswordHashingService passwordHasher)
    {
        return passwordHasher.VerifyPassword(rawPassword, PasswordHash);
    }
}
