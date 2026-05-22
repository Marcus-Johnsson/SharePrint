using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Payments;
using SharePrint.Infrastructure.Persistence;
using SharePrint.Infrastructure.Storage;

namespace SharePrint.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<SharePrintDbContext>(o =>
            o.UseNpgsql(cfg.GetConnectionString("DefaultConnection"),
                npg => npg.MigrationsAssembly(typeof(SharePrintDbContext).Assembly.GetName().Name)));

        services.AddIdentityCore<User>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<SharePrintDbContext>()
            .AddSignInManager();

        AddStorage(services, cfg);
        AddPayments(services, cfg);
        return services;
    }

    private static void AddStorage(IServiceCollection services, IConfiguration cfg)
    {
        var provider = (cfg["Storage:Provider"] ?? "Local").ToLowerInvariant();
        switch (provider)
        {
            case "cloud":
                services.AddSingleton<IFileStorage, CloudFileStorage>();
                services.AddSingleton<IPictureStorage, CloudPictureStorage>();
                break;
            case "local":
            case "localdisk":
                services.AddSingleton<IFileStorage>(_ =>
                    new LocalDiskStorage(cfg["Storage:FilesPath"] ?? "./data/files"));
                services.AddSingleton<IPictureStorage>(_ =>
                    new LocalDiskPictureStorage(cfg["Storage:PicturesPath"] ?? "./data/pictures"));
                break;
            default:
                throw new InvalidOperationException($"Unknown Storage:Provider '{provider}'. Expected 'Local' or 'S3'.");
        }
    }

    private static void AddPayments(IServiceCollection services, IConfiguration cfg)
    {
        var pay = cfg["Payment:Provider"];
        if (pay == "Fake")
            services.AddSingleton<IPaymentProcessor, FakePaymentProcessor>();
        else
            throw new InvalidOperationException($"Unknown Payment:Provider '{pay}'.");
    }
}