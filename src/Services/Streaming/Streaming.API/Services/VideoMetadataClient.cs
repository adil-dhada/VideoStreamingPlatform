using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Streaming.API.Services;

public class VideoMetadataClient : IVideoMetadataClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VideoMetadataClient> _logger;

    public VideoMetadataClient(HttpClient httpClient, ILogger<VideoMetadataClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<VideoMetadataDto?> GetMetadataAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/videos/{videoId}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VideoMetadataDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching video metadata for {VideoId}", videoId);
            return null;
            // Throwing might be better, returning null is safe fallback for access denied
        }
    }
}
