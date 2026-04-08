using Microsoft.EntityFrameworkCore.Storage;

namespace Cyber_Cord.Api.Types.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync();

    Task ExecuteInTransactionAsync(Func<IDbContextTransaction, Task> action);

    Task<T> ExecuteInTransactionAsync<T>(Func<IDbContextTransaction, Task<T>> action);
}
