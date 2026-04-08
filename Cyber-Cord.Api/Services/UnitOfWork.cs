using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Types.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cyber_Cord.Api.Services;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task ExecuteInTransactionAsync(Func<IDbContextTransaction, Task> action)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        await action(transaction);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<IDbContextTransaction, Task<T>> func)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        return await func(transaction);
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();
}
