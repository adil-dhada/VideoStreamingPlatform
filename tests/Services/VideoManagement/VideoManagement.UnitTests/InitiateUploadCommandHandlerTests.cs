using System;
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

public class InitiateUploadCommandHandlerTests
{
    private readonly Mock<IUploadSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IBlobStorageService> _blobStorageMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly InitiateUploadCommandHandler _handler;

    public InitiateUploadCommandHandlerTests()
    {
        _sessionRepositoryMock = new Mock<IUploadSessionRepository>();
        _blobStorageMock = new Mock<IBlobStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new InitiateUploadCommandHandler(
            _sessionRepositoryMock.Object,
            _blobStorageMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCreateUploadSessionAndVideo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new InitiateUploadCommand(userId, "test.mp4", 5000000, 1, "Title", "Description");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SessionId.Should().NotBeEmpty();
        result.ChunkSizeBytes.Should().BeGreaterThan(0);

        _sessionRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<UploadSession>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidTotalChunks_ShouldThrow()
    {
        // Arrange
        var command = new InitiateUploadCommand(Guid.NewGuid(), "test.mp4", 5000000, 0, "Title", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
