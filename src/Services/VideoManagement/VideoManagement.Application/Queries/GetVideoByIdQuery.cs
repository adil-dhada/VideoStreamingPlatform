using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.BuildingBlocks;
using VideoManagement.Domain;

namespace VideoManagement.Application.Queries;

public record VideoDetailDto(
    Guid Id,
    string Title,
    string? Description,
    Guid OwnerId,
    double? DurationSeconds,
    string Visibility,
    string Status,
    DateTimeOffset CreatedAt
);

public record GetVideoByIdQuery(Guid VideoId, Guid? RequestingUserId) : IQuery<VideoDetailDto>;

public class GetVideoByIdQueryHandler : MediatR.IRequestHandler<GetVideoByIdQuery, VideoDetailDto>
{
    private readonly IVideoRepository _videoRepository;

    public GetVideoByIdQueryHandler(IVideoRepository videoRepository)
    {
        _videoRepository = videoRepository;
    }

    public async Task<VideoDetailDto> Handle(GetVideoByIdQuery request, CancellationToken cancellationToken)
    {
        var video = await _videoRepository.GetByIdAsync(request.VideoId, cancellationToken);
        if (video == null)
            throw new InvalidOperationException("Video not found");

        if (video.Visibility == VideoVisibility.Private && video.OwnerId != request.RequestingUserId)
        {
            throw new UnauthorizedAccessException("Forbidden");
        }

        return new VideoDetailDto(
            video.Id,
            video.Title,
            video.Description,
            video.OwnerId,
            video.DurationSeconds,
            video.Visibility.ToString(),
            video.Status.ToString(),
            video.CreatedAt
        );
    }
}
