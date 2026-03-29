using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.BuildingBlocks;
using Streaming.API.Controllers;
using Streaming.API.Services;
using Xunit;

namespace Streaming.UnitTests;

public class StreamControllerTests
{
    private readonly Mock<IBlobStorageService> _blobStorageMock;
    private readonly Mock<IVideoMetadataClient> _metadataClientMock;
    private readonly StreamController _controller;

    public StreamControllerTests()
    {
        _blobStorageMock = new Mock<IBlobStorageService>();
        _metadataClientMock = new Mock<IVideoMetadataClient>();

        _controller = new StreamController(
            _blobStorageMock.Object,
            _metadataClientMock.Object
        );
    }

    [Fact]
    public async Task GetVideoStream_WithPublicVideo_ShouldReturnFile()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var metadata = new VideoMetadataDto(Guid.NewGuid(), "Public", "Ready");
        
        _metadataClientMock.Setup(c => c.GetMetadataAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
            
        var stream = new MemoryStream();
        _blobStorageMock.Setup(b => b.DownloadAsync($"videos/{videoId}/hls/master.m3u8", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        // Act
        var result = await _controller.GetVideoStream(videoId, "master.m3u8", CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("application/vnd.apple.mpegurl");
        fileResult.EnableRangeProcessing.Should().BeTrue();
    }

    [Fact]
    public async Task GetVideoStream_WithPrivateVideo_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var metadata = new VideoMetadataDto(Guid.NewGuid(), "Private", "Ready");
        
        _metadataClientMock.Setup(c => c.GetMetadataAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // Unauthenticated
        };

        // Act
        var result = await _controller.GetVideoStream(videoId, "master.m3u8", CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetVideoStream_WithPrivateVideo_AuthorizedOwner_ShouldReturnFile()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var metadata = new VideoMetadataDto(ownerId, "Private", "Ready");
        
        _metadataClientMock.Setup(c => c.GetMetadataAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
            
        var stream = new MemoryStream();
        _blobStorageMock.Setup(b => b.DownloadAsync($"videos/{videoId}/hls/master.m3u8", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ownerId.ToString())
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await _controller.GetVideoStream(videoId, "master.m3u8", CancellationToken.None);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
    }
}
