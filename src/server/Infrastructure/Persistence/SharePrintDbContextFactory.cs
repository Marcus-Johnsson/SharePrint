using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SharePrint.Infrastructure.Persistence;

// Design-time only: lets `dotnet ef` build the context without runtime DI.
// Connection string mirrors Api/appsettings.json ConnectionStrings:Default.
public class SharePrintDbContextFactory : IDesignTimeDbContextFactory<SharePrintDbContext>
{
    public SharePrintDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SharePrintDbContext>()
            .UseSqlite("Data Source=marketplace.db")
            .Options;
        return new SharePrintDbContext(options);
    }
}
