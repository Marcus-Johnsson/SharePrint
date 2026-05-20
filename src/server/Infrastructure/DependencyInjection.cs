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
            o.UseSqlite(cfg.GetConnectionString("Default")));
        
        services.AddIdentityCore<User>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<SharePrintDbContext>()
            .AddSignInManager();

        var storage = cfg["Storage:Provider"];
        if (storage == "LocalDisk")
            services.AddSingleton<IFileStorage>(_ => new
                LocalDiskStorage(cfg["Storage:LocalDisk:RootPath"] ?? "./storage"));
        else
        {
            throw new InvalidOperationException($"Error locating storage provider: {storage}");
        }
        
        var pay = cfg["Payment:Provider"];
        if (pay == "Fake")
            services.AddSingleton<IPaymentProcessor, FakePaymentProcessor>();
        else throw new InvalidOperationException($"Error locating payment provider: {pay}");
        return services;
    }
}