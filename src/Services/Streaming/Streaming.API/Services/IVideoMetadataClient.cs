using System;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.API.Services;

public record VideoMetadataDto(Guid OwnerId, string Visibility, string Status);

public interface IVideoMetadataClient
{
    Task<VideoMetadataDto?> GetMetadataAsync(Guid videoId, CancellationToken cancellationToken = default);
}
