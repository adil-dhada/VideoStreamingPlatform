using System;
using System.Threading;
using System.Threading.Tasks;

namespace Transcoding.Domain;

public interface ITranscodingJobRepository
{
    Task<TranscodingJob?> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default);
    Task AddAsync(TranscodingJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(TranscodingJob job, CancellationToken cancellationToken = default);
}
