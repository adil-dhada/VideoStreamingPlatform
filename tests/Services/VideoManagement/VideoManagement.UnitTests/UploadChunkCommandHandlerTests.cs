using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Shared.BuildingBlocks;
using VideoManagement.Application.Abstractions;
using VideoManagement.Application.Commands;
using VideoManagement.Domain;
using Xunit;

namespace VideoManagement.UnitTests;

public class UploadChunkCommandHandlerTests
{
    private readonly Mock<IUploadSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IBlobStorageService> _blobStorageMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UploadChunkCommandHandler _handler;

    public UploadChunkCommandHandlerTests()
    {
        _sessionRepositoryMock = new Mock<IUploadSessionRepository>();
        _blobStorageMock = new Mock<IBlobStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UploadChunkCommandHandler(
            _sessionRepositoryMock.Object,
            _blobStorageMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidChunk_ShouldAcknowledgeAndUploadToBlob()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = UploadSession.Create(userId, "test.mp4", 1000, 2, 1024);
        var command = new UploadChunkCommand(session.Id, userId, 0, Stream.Null);

        _sessionRepositoryMock.Setup(repo => repo.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalChunks.Should().Be(2);
        result.ReceivedCount.Should().Be(1);

        _blobStorageMock.Verify(b => b.UploadAsync(
            It.Is<string>(p => p.Contains("chunk_0000")), 
            Stream.Null, 
            "application/octet-stream", 
            It.IsAny<CancellationToken>()), Times.Once);

        _sessionRepositoryMock.Verify(repo => repo.UpdateAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidSession_ShouldThrow()
    {
        // Arrange
        var command = new UploadChunkCommand(Guid.NewGuid(), Guid.NewGuid(), 0, Stream.Null);
        _sessionRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UploadSession?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
