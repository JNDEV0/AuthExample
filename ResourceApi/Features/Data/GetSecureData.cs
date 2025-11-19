using Carter;
using Microsoft.AspNetCore.Authorization;

namespace ResourceApi.Features.Data;

public class GetSecureDataEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/data", [Authorize(Policy = "ApiScope")] () =>
        {
            return Results.Ok(new { Data = "This is secure data." });
        })
        .WithTags("Data");
    }
}
