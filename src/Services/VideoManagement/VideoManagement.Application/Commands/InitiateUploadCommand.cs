using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.BuildingBlocks;
using VideoManagement.Application.Abstractions;
using VideoManagement.Domain;

namespace VideoManagement.Application.Commands;

public record InitiateUploadDto(Guid SessionId, int ChunkSizeBytes);

public record InitiateUploadCommand(
    Guid UserId,
    string FileName,
    long FileSizeBytes,
    int TotalChunks,
    string Title,
    string? Description
) : ICommand<InitiateUploadDto>;

public class InitiateUploadCommandHandler : MediatR.IRequestHandler<InitiateUploadCommand, InitiateUploadDto>
{
    private readonly IUploadSessionRepository _sessionRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly IUnitOfWork _unitOfWork;

    public InitiateUploadCommandHandler(
        IUploadSessionRepository sessionRepository,
        IBlobStorageService blobStorage,
        IUnitOfWork unitOfWork)
    {
        _sessionRepository = sessionRepository;
        _blobStorage = blobStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<InitiateUploadDto> Handle(InitiateUploadCommand request, CancellationToken cancellationToken)
    {
        const int DefaultChunkSize = 5 * 1024 * 1024; // 5 MB

        var session = UploadSession.Create(
            request.UserId,
            request.FileName,
            request.FileSizeBytes,
            request.TotalChunks,
            DefaultChunkSize);

        await _sessionRepository.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new InitiateUploadDto(session.Id, session.ChunkSizeBytes);
    }
}
