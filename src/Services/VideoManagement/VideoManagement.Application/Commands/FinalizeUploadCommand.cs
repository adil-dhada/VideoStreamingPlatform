using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Shared.BuildingBlocks;
using Shared.Contracts;
using VideoManagement.Application.Abstractions;
using VideoManagement.Domain;

namespace VideoManagement.Application.Commands;

public record FinalizeUploadCommand(Guid SessionId, Guid UserId, string Title, string? Description) : ICommand<Guid>;

public class FinalizeUploadCommandHandler : MediatR.IRequestHandler<FinalizeUploadCommand, Guid>
{
    private readonly IUploadSessionRepository _sessionRepository;
    private readonly IVideoRepository _videoRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public FinalizeUploadCommandHandler(
        IUploadSessionRepository sessionRepository,
        IVideoRepository videoRepository,
        IBlobStorageService blobStorage,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _sessionRepository = sessionRepository;
        _videoRepository = videoRepository;
        _blobStorage = blobStorage;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> Handle(FinalizeUploadCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session == null || session.UserId != request.UserId)
            throw new InvalidOperationException("Session not found or forbidden.");

        if (!session.IsComplete())
            throw new InvalidOperationException("Not all chunks have been received.");

        string rawFilePath = $"raw/{Guid.NewGuid()}/{session.FileName}";

        // Note: For large files, saving entirely to MemoryStream is bad. 
        // We use a temporary local file or pipe directly if Blob Storage supports append.
        var tempFile = Path.GetTempFileName();
        try
        {
            using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                for (int i = 0; i < session.TotalChunks; i++)
                {
                    string chunkPath = $"{session.TempDirectory}/chunk_{i:D4}";
                    using var chunkStream = await _blobStorage.DownloadAsync(chunkPath, cancellationToken);
                    await chunkStream.CopyToAsync(fs, cancellationToken);
                }
            }

            using (var readFs = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await _blobStorage.UploadAsync(rawFilePath, readFs, "video/mp4", cancellationToken);
            }
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }

        await _blobStorage.DeleteDirectoryAsync(session.TempDirectory, cancellationToken);

        var video = Video.Create(request.UserId, request.Title, request.Description, rawFilePath, session.TotalFileSizeBytes);
        await _videoRepository.AddAsync(video, cancellationToken);

        session.Complete();
        await _sessionRepository.UpdateAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _publishEndpoint.Publish(new VideoUploadedMessage(
            video.Id,
            video.OwnerId,
            rawFilePath,
            session.FileName,
            video.CreatedAt.UtcDateTime
        ), cancellationToken);

        return video.Id;
    }
}
