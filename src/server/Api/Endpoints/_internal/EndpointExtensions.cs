namespace SharePrint.Api.Endpoints._internal;

public static class EndpointExtensions
{
    public static void MapEndpoints<T>(this IEndpointRouteBuilder app)
        => MapEndpoints(app, typeof(T));

    public static void MapEndpoints(this IEndpointRouteBuilder app, Type marker)
    {
        var endpointTypes = marker.Assembly.DefinedTypes
            .Where(x => !x.IsAbstract
                     && !x.IsInterface
                     && typeof(IEndpoint).IsAssignableFrom(x));

        foreach (var endpointType in endpointTypes)
        {
            endpointType.GetMethod(nameof(IEndpoint.MapEndpoint))!
                .Invoke(null, new object[] { app });
        }
    }
}
