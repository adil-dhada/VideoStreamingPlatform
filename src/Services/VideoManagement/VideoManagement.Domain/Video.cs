using System;
using System.Collections.Generic;
using Shared.BuildingBlocks;

namespace VideoManagement.Domain;

public class Video : AggregateRoot<Guid>
{
    public Guid OwnerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public VideoStatus Status { get; private set; }
    public VideoVisibility Visibility { get; private set; }
    public long FileSizeBytes { get; private set; }
    public double? DurationSeconds { get; private set; }
    public string? RawFilePath { get; private set; }
    public string? TranscodingError { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    private readonly List<VideoVariant> _variants = new();
    public IReadOnlyList<VideoVariant> Variants => _variants.AsReadOnly();

    private Video() { }

    private Video(Guid id, Guid ownerId, string title, string? description, string rawFilePath, long fileSizeBytes)
    {
        Id = id;
        OwnerId = ownerId;
        Title = title;
        Description = description;
        RawFilePath = rawFilePath;
        FileSizeBytes = fileSizeBytes;
        Status = VideoStatus.Pending;
        Visibility = VideoVisibility.Private;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Video Create(Guid ownerId, string title, string? description, string rawFilePath, long fileSize)
    {
        var video = new Video(Guid.NewGuid(), ownerId, title, description, rawFilePath, fileSize);
        return video;
    }

    public void StartTranscoding()
    {
        Status = VideoStatus.Processing;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CompleteTranscoding(IEnumerable<VideoVariant> variants, double duration)
    {
        Status = VideoStatus.Ready;
        DurationSeconds = duration;
        _variants.AddRange(variants);
        UpdatedAt = DateTimeOffset.UtcNow;
        
        if (Visibility == VideoVisibility.Public)
        {
            PublishedAt = DateTimeOffset.UtcNow;
        }
    }

    public void FailTranscoding(string reason)
    {
        Status = VideoStatus.Failed;
        TranscodingError = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MakePublic()
    {
        Visibility = VideoVisibility.Public;
        PublishedAt ??= DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MakePrivate()
    {
        Visibility = VideoVisibility.Private;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateMetadata(string title, string? description)
    {
        Title = title;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete()
    {
        Status = VideoStatus.Deleted;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
