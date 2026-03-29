using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using Moq;
using Shared.BuildingBlocks;
using Shared.Contracts;
using VideoManagement.Application.Abstractions;
using VideoManagement.Application.Commands;
using VideoManagement.Domain;
using Xunit;

namespace VideoManagement.UnitTests;

public class FinalizeUploadCommandHandlerTests
{
    private readonly Mock<IUploadSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IVideoRepository> _videoRepositoryMock;
    private readonly Mock<IBlobStorageService> _blobStorageMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly FinalizeUploadCommandHandler _handler;

    public FinalizeUploadCommandHandlerTests()
    {
        _sessionRepositoryMock = new Mock<IUploadSessionRepository>();
        _videoRepositoryMock = new Mock<IVideoRepository>();
        _blobStorageMock = new Mock<IBlobStorageService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();

        _handler = new FinalizeUploadCommandHandler(
            _sessionRepositoryMock.Object,
            _videoRepositoryMock.Object,
            _blobStorageMock.Object,
            _unitOfWorkMock.Object,
            _publishEndpointMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithAllChunksReceived_ShouldAssembleAndPublish()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = UploadSession.Create(userId, "test.mp4", 1000, 2, 512);
        
        // Mark chunks as received
        session.RecordChunkReceived(0);
        session.RecordChunkReceived(1);

        var command = new FinalizeUploadCommand(session.Id, userId, "Title", "Desc");

        _sessionRepositoryMock.Setup(repo => repo.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _blobStorageMock.Setup(b => b.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(new byte[512])));

        _blobStorageMock.Setup(b => b.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _blobStorageMock.Setup(b => b.DeleteDirectoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        session.Status.Should().Be(UploadSessionStatus.Completed);

        _videoRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Video>(), It.IsAny<CancellationToken>()), Times.Once);
        _blobStorageMock.Verify(b => b.UploadAsync(It.Is<string>(s => s.StartsWith("raw/")), It.IsAny<Stream>(), "video/mp4", It.IsAny<CancellationToken>()), Times.Once);
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<VideoUploadedMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMissingChunks_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = UploadSession.Create(userId, "test.mp4", 1000, 2, 512);
        session.RecordChunkReceived(0); // missing chunk 1
        
        var command = new FinalizeUploadCommand(session.Id, userId, "Title", "Desc");

        _sessionRepositoryMock.Setup(repo => repo.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
