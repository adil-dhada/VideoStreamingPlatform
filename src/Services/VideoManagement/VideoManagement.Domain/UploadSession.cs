using System;
using System.Collections.Generic;
using Shared.BuildingBlocks;
using System.Text.Json;

namespace VideoManagement.Domain;

public class UploadSession : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public long TotalFileSizeBytes { get; private set; }
    public int TotalChunks { get; private set; }
    public int ChunkSizeBytes { get; private set; }
    
    public string ReceivedChunksJson { get; private set; } = "[]";
    
    public UploadSessionStatus Status { get; private set; }
    public string TempDirectory { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private UploadSession() { }

    private UploadSession(Guid id, Guid userId, string fileName, long totalFileSizeBytes, int totalChunks, int chunkSizeBytes)
    {
        Id = id;
        UserId = userId;
        FileName = fileName;
        TotalFileSizeBytes = totalFileSizeBytes;
        TotalChunks = totalChunks;
        ChunkSizeBytes = chunkSizeBytes;
        Status = UploadSessionStatus.Active;
        TempDirectory = $"uploads/{id}";
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(24);
    }

    public static UploadSession Create(Guid userId, string fileName, long fileSize, int totalChunks, int chunkSize)
    {
        return new UploadSession(Guid.NewGuid(), userId, fileName, fileSize, totalChunks, chunkSize);
    }

    public List<int> GetReceivedChunks()
    {
        return JsonSerializer.Deserialize<List<int>>(ReceivedChunksJson) ?? new List<int>();
    }

    public void RecordChunkReceived(int chunkIndex)
    {
        var chunks = GetReceivedChunks();
        if (!chunks.Contains(chunkIndex))
        {
            chunks.Add(chunkIndex);
            ReceivedChunksJson = JsonSerializer.Serialize(chunks);
        }
    }

    public bool IsComplete()
    {
        return GetReceivedChunks().Count == TotalChunks;
    }

    public void Complete()
    {
        Status = UploadSessionStatus.Completed;
    }

    public void Abort()
    {
        Status = UploadSessionStatus.Aborted;
    }
}
