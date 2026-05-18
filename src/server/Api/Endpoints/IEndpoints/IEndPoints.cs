namespace SharePrint.Api.Endpoints.IEndpoints;

public interface IEndpoint
{
    public static abstract void MapEndpoint(IEndpointRouteBuilder app);
}