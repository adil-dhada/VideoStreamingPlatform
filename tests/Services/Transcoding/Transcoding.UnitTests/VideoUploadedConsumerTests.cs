using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Moq;
using Shared.Contracts;
using Transcoding.Application.Abstractions;
using Transcoding.Application.Consumers;
using Transcoding.Domain;
using Xunit;

namespace Transcoding.UnitTests;

public class VideoUploadedConsumerTests
{
    private readonly Mock<ITranscodingJobRepository> _jobRepositoryMock;
    private readonly Mock<IFFmpegTranscodingService> _ffmpegServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly VideoUploadedConsumer _consumer;

    public VideoUploadedConsumerTests()
    {
        _jobRepositoryMock = new Mock<ITranscodingJobRepository>();
        _ffmpegServiceMock = new Mock<IFFmpegTranscodingService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();

        _consumer = new VideoUploadedConsumer(
            _jobRepositoryMock.Object,
            _ffmpegServiceMock.Object,
            _unitOfWorkMock.Object,
            _publishEndpointMock.Object
        );
    }

    [Fact]
    public async Task Consume_WithNewJob_ShouldProcessAndPublish()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var msg = new VideoUploadedMessage(videoId, Guid.NewGuid(), "path", "file.mp4", DateTime.UtcNow);
        var contextMock = new Mock<ConsumeContext<VideoUploadedMessage>>();
        contextMock.Setup(c => c.Message).Returns(msg);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        _jobRepositoryMock.Setup(repo => repo.GetByVideoIdAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranscodingJob?)null);

        var variants = new List<TranscodedVariant> { new TranscodedVariant("360p", "path", "dir", 1000, 640, 360) };
        _ffmpegServiceMock.Setup(s => s.TranscodeAsync(videoId, "path", It.IsAny<CancellationToken>()))
            .ReturnsAsync((variants.AsReadOnly(), 120.0));

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        _jobRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<TranscodingJob>(), It.IsAny<CancellationToken>()), Times.Once);
        _jobRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<TranscodingJob>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _publishEndpointMock.Verify(p => p.Publish(It.Is<TranscodingCompletedMessage>(m => m.VideoId == videoId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
