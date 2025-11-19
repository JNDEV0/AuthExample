using Carter;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace AuthServer.Features.Identity.CheckEmail;

public class CheckEmailEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/check-email", Handle);
    }

    private async Task<IResult> Handle(CheckEmailRequest request, UserManager<Data.ApplicationUser> userManager, IDataProtectionProvider protectionProvider, HttpContext context)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Results.NotFound();
        }

        var protector = protectionProvider.CreateProtector("Authentication.Login.Challenge:" + request.Email);
        var token = protector.Protect(DateTime.UtcNow.ToString());

        var redirectUrl = $"https://localhost:5002/Account/LoginPassword?token={token}&email={request.Email}";
        // context.Response.Headers["HX-Redirect"] = redirectUrl;
        return Results.Ok(new { token });
    }
}

public record CheckEmailRequest(string Email);
