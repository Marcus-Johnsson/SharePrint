namespace SharePrint.Api.Endpoints._IEndpoints;

public interface IEndpoint
{
    public static abstract void MapEndpoint(IEndpointRouteBuilder app);
}