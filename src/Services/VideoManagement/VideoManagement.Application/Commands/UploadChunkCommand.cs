using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shared.BuildingBlocks;
using VideoManagement.Application.Abstractions;
using VideoManagement.Domain;

namespace VideoManagement.Application.Commands;

public record ChunkAckDto(int ReceivedCount, int TotalChunks);

public record UploadChunkCommand(
    Guid SessionId,
    Guid UserId,
    int ChunkIndex,
    Stream Data
) : ICommand<ChunkAckDto>;

public class UploadChunkCommandHandler : MediatR.IRequestHandler<UploadChunkCommand, ChunkAckDto>
{
    private readonly IUploadSessionRepository _sessionRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly IUnitOfWork _unitOfWork;

    public UploadChunkCommandHandler(
        IUploadSessionRepository sessionRepository,
        IBlobStorageService blobStorage,
        IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _blobStorage = blobStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChunkAckDto> Handle(UploadChunkCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session == null || session.UserId != request.UserId)
            throw new InvalidOperationException("Session not found or forbidden.");

        if (session.Status != UploadSessionStatus.Active || session.ExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Session is not active or has expired.");

        if (request.ChunkIndex < 0 || request.ChunkIndex >= session.TotalChunks)
            throw new ArgumentOutOfRangeException(nameof(request.ChunkIndex));

        string chunkPath = $"{session.TempDirectory}/chunk_{request.ChunkIndex:D4}";

        await _blobStorage.UploadAsync(chunkPath, request.Data, "application/octet-stream", cancellationToken);

        session.RecordChunkReceived(request.ChunkIndex);
        
        await _sessionRepository.UpdateAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChunkAckDto(session.GetReceivedChunks().Count, session.TotalChunks);
    }
}
