using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks;
using Shared.Contracts;
using Transcoding.Application.Abstractions;

namespace Transcoding.Infrastructure.Services;

public class FFmpegTranscodingService : IFFmpegTranscodingService
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<FFmpegTranscodingService> _logger;

    public FFmpegTranscodingService(IBlobStorageService blobStorage, ILogger<FFmpegTranscodingService> logger)
    {
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<TranscodedVariant> Variants, double Duration)> TranscodeAsync(Guid videoId, string rawFilePath, CancellationToken ct)
    {
        var tempSourceFile = Path.GetTempFileName();
        try
        {
            using (var sourceFs = new FileStream(tempSourceFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using var blobStream = await _blobStorage.DownloadAsync(rawFilePath, ct);
                await blobStream.CopyToAsync(sourceFs, ct);
            }

            double duration = 120.0;
            
            var variants = new List<TranscodedVariant>();
            var workDir = Path.Combine(Path.GetTempPath(), videoId.ToString());
            Directory.CreateDirectory(workDir);

            var tier360p = new { Name = "360p", Width = 640, Height = 360, Vb = 800, Ab = 96 };
            
            string outDir = Path.Combine(workDir, tier360p.Name);
            Directory.CreateDirectory(outDir);

            string indexManifest = Path.Combine(outDir, "index.m3u8");
            string segmentPattern = Path.Combine(outDir, "seg%03d.ts");

            var args = $"-y -i \"{tempSourceFile}\" -vf scale={tier360p.Width}:{tier360p.Height} -c:v libx264 -preset fast -crf 23 -b:v {tier360p.Vb}k -c:a aac -b:a {tier360p.Ab}k -hls_time 6 -hls_playlist_type vod -hls_segment_filename \"{segmentPattern}\" \"{indexManifest}\"";
            
            _logger.LogInformation("Running FFmpeg: {args}", args);
            
            var psi = new ProcessStartInfo("ffmpeg", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            if (process == null) throw new InvalidOperationException("Failed to start FFmpeg process.");

            await process.WaitForExitAsync(ct);
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                throw new InvalidOperationException($"FFmpeg failed: {error}");
            }

            var segmentFiles = Directory.GetFiles(outDir, "seg*.ts");
            foreach (var segFile in segmentFiles)
            {
                using var sf = File.OpenRead(segFile);
                string segPath = $"videos/{videoId}/hls/{tier360p.Name}/{Path.GetFileName(segFile)}";
                await _blobStorage.UploadAsync(segPath, sf, "video/MP2T", ct);
            }

            using (var idxStream = File.OpenRead(indexManifest))
            {
                string idxPath = $"videos/{videoId}/hls/{tier360p.Name}/index.m3u8";
                await _blobStorage.UploadAsync(idxPath, idxStream, "application/vnd.apple.mpegurl", ct);
            }

            variants.Add(new TranscodedVariant(
                tier360p.Name,
                $"videos/{videoId}/hls/{tier360p.Name}/index.m3u8",
                $"videos/{videoId}/hls/{tier360p.Name}/",
                tier360p.Vb + tier360p.Ab,
                tier360p.Width,
                tier360p.Height
            ));

            var masterContent = $"#EXTM3U\n#EXT-X-VERSION:3\n#EXT-X-STREAM-INF:BANDWIDTH={(tier360p.Vb + tier360p.Ab) * 1000},RESOLUTION={tier360p.Width}x{tier360p.Height}\n/api/stream/{videoId}/{tier360p.Name}/index.m3u8\n";
            string masterPath = $"videos/{videoId}/hls/master.m3u8";
            
            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(masterContent)))
            {
                await _blobStorage.UploadAsync(masterPath, ms, "application/vnd.apple.mpegurl", ct);
            }

            Directory.Delete(workDir, true);

            return (variants.AsReadOnly(), duration);
        }
        finally
        {
            if (File.Exists(tempSourceFile)) File.Delete(tempSourceFile);
        }
    }
}
