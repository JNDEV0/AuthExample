using Carter;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace AuthServer.Features.Identity.Register;

public class RegisterStartEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register-start", Handle);
    }

    private async Task<IResult> Handle(RegisterStartRequest request, UserManager<Data.ApplicationUser> userManager, IDataProtectionProvider protectionProvider, HttpContext context)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
            // Don't alert the client that the user exists.
            // Send a password reset email instead.
            // Not implemented yet.
            return Results.Ok();
        }

        // Send confirmation code to email.
        // Not implemented yet.

        var protector = protectionProvider.CreateProtector("Authentication.Register.Challenge:" + request.Email);
        var token = protector.Protect(DateTime.UtcNow.ToString());
        
        var redirectUrl = $"https://localhost:5002/Account/RegisterEnterCode?token={token}&email={request.Email}";
        // context.Response.Headers["HX-Redirect"] = redirectUrl;
        return Results.Ok(new { token });
    }
}

public record RegisterStartRequest(string Email, DateTime DateOfBirth, bool TermsAccepted);
