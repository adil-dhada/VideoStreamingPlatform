using System;
using System.Threading;
using System.Threading.Tasks;

namespace VideoManagement.Domain;

public interface IVideoRepository
{
    Task<Video?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Video video, CancellationToken cancellationToken = default);
    Task UpdateAsync(Video video, CancellationToken cancellationToken = default);
    Task DeleteAsync(Video video, CancellationToken cancellationToken = default);
}
