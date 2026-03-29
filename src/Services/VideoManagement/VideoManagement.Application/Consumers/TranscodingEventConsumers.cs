using System;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Shared.Contracts;
using VideoManagement.Application.Abstractions;
using VideoManagement.Domain;

namespace VideoManagement.Application.Consumers;

public class TranscodingCompletedConsumer : IConsumer<TranscodingCompletedMessage>
{
    private readonly IVideoRepository _videoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TranscodingCompletedConsumer(IVideoRepository videoRepository, IUnitOfWork unitOfWork)
    {
        _videoRepository = videoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<TranscodingCompletedMessage> context)
    {
        var msg = context.Message;
        var video = await _videoRepository.GetByIdAsync(msg.VideoId, context.CancellationToken);
        if (video != null)
        {
            var variants = msg.Variants.Select(v => new VideoVariant(
                Guid.NewGuid(),
                msg.VideoId,
                Enum.Parse<ResolutionTier>("R" + v.Resolution),
                v.BitrateKbps,
                v.Width,
                v.Height,
                v.ManifestPath,
                v.SegmentDirectory
            )).ToList();

            video.CompleteTranscoding(variants, msg.DurationSeconds);
            await _videoRepository.UpdateAsync(video, context.CancellationToken);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}

public class TranscodingFailedConsumer : IConsumer<TranscodingFailedMessage>
{
    private readonly IVideoRepository _videoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TranscodingFailedConsumer(IVideoRepository videoRepository, IUnitOfWork unitOfWork)
    {
        _videoRepository = videoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<TranscodingFailedMessage> context)
    {
        var msg = context.Message;
        var video = await _videoRepository.GetByIdAsync(msg.VideoId, context.CancellationToken);
        if (video != null)
        {
            video.FailTranscoding(msg.Reason);
            await _videoRepository.UpdateAsync(video, context.CancellationToken);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
    }
}
