using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;

namespace SharePrint.Api.Endpoints.Pictures;

public class GetPictures : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/pictures/{key}", Handler)
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithName("GetPicture");
    }

    private static async Task<IResult> Handler(
        string key,
        IPictureStorage pictureStorage,
        HttpContext httpContext)
    {
        try
        {
            var stored = await pictureStorage.OpenReadAsync(key, httpContext.RequestAborted);
            httpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.Stream(stored.Content, stored.ContentType);
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    }
}