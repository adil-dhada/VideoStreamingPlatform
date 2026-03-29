using System.Threading;
using System.Threading.Tasks;

namespace Identity.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
