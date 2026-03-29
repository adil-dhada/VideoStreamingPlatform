using System;
using Shared.BuildingBlocks;

namespace Transcoding.Domain;

public class TranscodingJob : AggregateRoot<Guid>
{
    public Guid VideoId { get; private set; }
    public TranscodingJobStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TranscodingJob() { }

    private TranscodingJob(Guid id, Guid videoId)
    {
        Id = id;
        VideoId = videoId;
        Status = TranscodingJobStatus.Queued;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static TranscodingJob Create(Guid videoId)
    {
        return new TranscodingJob(Guid.NewGuid(), videoId);
    }

    public void Start()
    {
        Status = TranscodingJobStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        Status = TranscodingJobStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string reason)
    {
        Status = TranscodingJobStatus.Failed;
        ErrorMessage = reason;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
