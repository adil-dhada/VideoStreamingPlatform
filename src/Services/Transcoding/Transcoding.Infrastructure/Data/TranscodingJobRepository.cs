using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Transcoding.Domain;

namespace Transcoding.Infrastructure.Data;

public class TranscodingJobRepository : ITranscodingJobRepository
{
    private readonly TranscodingDbContext _context;

    public TranscodingJobRepository(TranscodingDbContext context)
    {
        _context = context;
    }

    public Task<TranscodingJob?> GetByVideoIdAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        return _context.TranscodingJobs.FirstOrDefaultAsync(j => j.VideoId == videoId, cancellationToken);
    }

    public async Task AddAsync(TranscodingJob job, CancellationToken cancellationToken = default)
    {
        await _context.TranscodingJobs.AddAsync(job, cancellationToken);
    }

    public Task UpdateAsync(TranscodingJob job, CancellationToken cancellationToken = default)
    {
        _context.TranscodingJobs.Update(job);
        return Task.CompletedTask;
    }
}
