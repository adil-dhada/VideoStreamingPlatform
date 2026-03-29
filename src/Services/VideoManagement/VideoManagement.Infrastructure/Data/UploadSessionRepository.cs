using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using VideoManagement.Domain;

namespace VideoManagement.Infrastructure.Data;

public class UploadSessionRepository : IUploadSessionRepository
{
    private readonly VideoManagementDbContext _dbContext;

    public UploadSessionRepository(VideoManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UploadSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(UploadSession session, CancellationToken cancellationToken = default)
    {
        await _dbContext.UploadSessions.AddAsync(session, cancellationToken);
    }

    public Task UpdateAsync(UploadSession session, CancellationToken cancellationToken = default)
    {
        _dbContext.UploadSessions.Update(session);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(UploadSession session, CancellationToken cancellationToken = default)
    {
        _dbContext.UploadSessions.Remove(session);
        return Task.CompletedTask;
    }
}
