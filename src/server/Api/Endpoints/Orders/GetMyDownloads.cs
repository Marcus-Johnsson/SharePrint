using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Orders;

public class GetMyDownloads : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/listings/{id}", Handler)
            .RequireAuthorization()
            .WithName("GetMyDownloads")
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private async Task<IResult> Handler(
        UserManager<User> users,
        HttpContext context,
        SharePrintDbContext db)
    {
        var user = await users.GetUserAsync(context.User);
        
        var downloads = await db.OrderItems
            .Include(i => i.Grant)
            .Where(i => i.DownloadPath && i.Grant != null)
            
            
    }
}