using System;
using Shared.BuildingBlocks;

namespace VideoManagement.Domain;

public class VideoVariant : Entity<Guid>
{
    public Guid VideoId { get; private set; }
    public ResolutionTier Resolution { get; private set; }
    public int BitrateKbps { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string ManifestPath { get; private set; } = string.Empty;
    public string SegmentDirectory { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private VideoVariant() { } // EF Core

    public VideoVariant(Guid id, Guid videoId, ResolutionTier resolution, int bitrateKbps, int width, int height, string manifestPath, string segmentDirectory)
    {
        Id = id;
        VideoId = videoId;
        Resolution = resolution;
        BitrateKbps = bitrateKbps;
        Width = width;
        Height = height;
        ManifestPath = manifestPath;
        SegmentDirectory = segmentDirectory;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
