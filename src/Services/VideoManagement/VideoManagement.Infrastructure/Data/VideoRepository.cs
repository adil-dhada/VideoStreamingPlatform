using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using VideoManagement.Domain;

namespace VideoManagement.Infrastructure.Data;

public class VideoRepository : IVideoRepository
{
    private readonly VideoManagementDbContext _dbContext;

    public VideoRepository(VideoManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Video?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Videos.Include(v => v.Variants).FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AddAsync(Video video, CancellationToken cancellationToken = default)
    {
        await _dbContext.Videos.AddAsync(video, cancellationToken);
    }

    public Task UpdateAsync(Video video, CancellationToken cancellationToken = default)
    {
        _dbContext.Videos.Update(video);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Video video, CancellationToken cancellationToken = default)
    {
        _dbContext.Videos.Remove(video);
        return Task.CompletedTask;
    }
}
