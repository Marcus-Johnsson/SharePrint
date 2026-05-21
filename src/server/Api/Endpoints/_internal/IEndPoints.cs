namespace SharePrint.Api.Endpoints._internal;

public interface IEndpoint
{
    public static abstract void MapEndpoint(IEndpointRouteBuilder app);
}