using System;
using System.Threading;
using System.Threading.Tasks;

namespace VideoManagement.Domain;

public interface IUploadSessionRepository
{
    Task<UploadSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(UploadSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(UploadSession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(UploadSession session, CancellationToken cancellationToken = default);
}
