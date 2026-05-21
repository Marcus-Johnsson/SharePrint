using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SharePrint.Infrastructure.Persistence;

// Design-time only: lets `dotnet ef` build the context without runtime DI.
// Reads connection string from Api/appsettings.{Environment}.json so dev and prod stay in sync.
public class SharePrintDbContextFactory : IDesignTimeDbContextFactory<SharePrintDbContext>
{
    public SharePrintDbContext CreateDbContext(string[] args)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var cfg = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = cfg.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                $"ConnectionStrings:DefaultConnection not found for environment '{env}'. " +
                $"Looked in: {Path.Combine(Directory.GetCurrentDirectory(), "../Api")}");

        var options = new DbContextOptionsBuilder<SharePrintDbContext>()
            .UseNpgsql(connectionString,
                npg => npg.MigrationsAssembly(typeof(SharePrintDbContext).Assembly.GetName().Name))
            .Options;

        return new SharePrintDbContext(options);
    }
}
