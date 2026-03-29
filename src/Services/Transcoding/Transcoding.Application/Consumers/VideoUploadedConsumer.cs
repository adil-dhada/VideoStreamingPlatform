using System;
using System.Threading.Tasks;
using MassTransit;
using Shared.Contracts;
using Transcoding.Application.Abstractions;
using Transcoding.Domain;

namespace Transcoding.Application.Consumers;

public class VideoUploadedConsumer : IConsumer<VideoUploadedMessage>
{
    private readonly ITranscodingJobRepository _jobRepository;
    private readonly IFFmpegTranscodingService _ffmpegService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public VideoUploadedConsumer(
        ITranscodingJobRepository jobRepository,
        IFFmpegTranscodingService ffmpegService,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _jobRepository = jobRepository;
        _ffmpegService = ffmpegService;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<VideoUploadedMessage> context)
    {
        var msg = context.Message;
        
        var existingJob = await _jobRepository.GetByVideoIdAsync(msg.VideoId, context.CancellationToken);
        if (existingJob != null && existingJob.Status == TranscodingJobStatus.Completed)
        {
            return;
        }

        var job = existingJob ?? TranscodingJob.Create(msg.VideoId);
        if (existingJob == null)
        {
            await _jobRepository.AddAsync(job, context.CancellationToken);
        }

        job.Start();
        await _jobRepository.UpdateAsync(job, context.CancellationToken);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);

        try
        {
            var (variants, duration) = await _ffmpegService.TranscodeAsync(msg.VideoId, msg.RawFilePath, context.CancellationToken);
            
            job.Complete();
            await _jobRepository.UpdateAsync(job, context.CancellationToken);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);

            await _publishEndpoint.Publish(new TranscodingCompletedMessage(
                msg.VideoId,
                variants,
                duration
            ), context.CancellationToken);
        }
        catch (Exception ex)
        {
            job.Fail(ex.Message);
            await _jobRepository.UpdateAsync(job, context.CancellationToken);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);

            await _publishEndpoint.Publish(new TranscodingFailedMessage(
                msg.VideoId,
                ex.Message
            ), context.CancellationToken);

            throw; 
        }
    }
}
