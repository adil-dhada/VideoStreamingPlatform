using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.BuildingBlocks;
using Streaming.API.Services;

namespace Streaming.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StreamController : ControllerBase
{
    private readonly IBlobStorageService _blobStorage;
    private readonly IVideoMetadataClient _metadataClient;

    public StreamController(IBlobStorageService blobStorage, IVideoMetadataClient metadataClient)
    {
        _blobStorage = blobStorage;
        _metadataClient = metadataClient;
    }

    [AllowAnonymous]
    [HttpGet("{videoId}/{*path}")]
    public async Task<IActionResult> GetVideoStream(Guid videoId, string path, CancellationToken cancellationToken)
    {
        var metadata = await _metadataClient.GetMetadataAsync(videoId, cancellationToken);
        if (metadata == null || metadata.Status != "Ready")
            return NotFound("Video not found or not ready.");

        if (metadata.Visibility == "Private")
        {
            if (!User.Identity?.IsAuthenticated == true)
                return Unauthorized();
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || Guid.Parse(userIdClaim) != metadata.OwnerId)
                return Forbid();
        }

        string blobPath = $"videos/{videoId}/hls/{path}";

        try
        {
            var stream = await _blobStorage.DownloadAsync(blobPath, cancellationToken);
            
            var ext = Path.GetExtension(path).ToLowerInvariant();
            string contentType = ext switch
            {
                ".m3u8" => "application/vnd.apple.mpegurl",
                ".ts" => "video/MP2T",
                _ => "application/octet-stream"
            };

            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}
