namespace SharePrint.Api.Endpoints;

public static class AuthEndpoints : IEndpoints
{
    public static void MapAuth()
    {
        var g = app.MapGroup("/api/auth");
    }
}