using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shared.Contracts;

namespace Transcoding.Application.Abstractions;

public interface IFFmpegTranscodingService
{
    Task<(IReadOnlyList<TranscodedVariant> Variants, double Duration)> TranscodeAsync(Guid videoId, string rawFilePath, CancellationToken ct);
}
