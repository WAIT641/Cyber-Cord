using Cyber_Cord.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Cyber_Cord.Api.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
