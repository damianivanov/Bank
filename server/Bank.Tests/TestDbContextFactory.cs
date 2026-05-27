using Bank.DB;
using Microsoft.EntityFrameworkCore;

namespace Bank.Tests;

internal static class TestDbContextFactory
{
    public static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var dbContext = new AppDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }
}
