using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoManagement.Application.Commands;

namespace VideoManagement.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IMediator _mediator;

    public UploadController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateUpload([FromBody] InitiateUploadRequest request)
    {
        var userId = GetUserId();
        var cmd = new InitiateUploadCommand(userId, request.FileName, request.FileSizeBytes, request.TotalChunks, request.Title, request.Description);
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpPut("{sessionId}/chunk/{chunkIndex}")]
    public async Task<IActionResult> UploadChunk(Guid sessionId, int chunkIndex)
    {
        var userId = GetUserId();
        var cmd = new UploadChunkCommand(sessionId, userId, chunkIndex, Request.Body);
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpPost("{sessionId}/finalize")]
    public async Task<IActionResult> FinalizeUpload(Guid sessionId, [FromBody] FinalizeUploadRequest request)
    {
        var userId = GetUserId();
        var cmd = new FinalizeUploadCommand(sessionId, userId, request.Title, request.Description);
        var videoId = await _mediator.Send(cmd);
        return Accepted(new { VideoId = videoId });
    }

    private Guid GetUserId()
    {
        var val = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return val != null ? Guid.Parse(val) : Guid.Empty;
    }
}

public record InitiateUploadRequest(string FileName, long FileSizeBytes, int TotalChunks, string Title, string? Description);
public record FinalizeUploadRequest(string Title, string? Description);
