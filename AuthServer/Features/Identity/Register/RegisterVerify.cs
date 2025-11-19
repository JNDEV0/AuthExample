using Carter;
using Microsoft.AspNetCore.DataProtection;

namespace AuthServer.Features.Identity.Register;

public class RegisterVerifyEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register-verify", Handle);
    }

    private IResult Handle(RegisterVerifyRequest request, IDataProtectionProvider protectionProvider, HttpContext context)
    {
        // Verify the code. Not implemented yet.

        var protector = protectionProvider.CreateProtector("Authentication.Register.PasswordChallenge:" + request.Email);
        var token = protector.Protect(DateTime.UtcNow.ToString());

        var redirectUrl = $"https://localhost:5002/Account/RegisterPassword?token={token}&email={request.Email}";
        // context.Response.Headers["HX-Redirect"] = redirectUrl;
        return Results.Ok(new { token });
    }
}

public record RegisterVerifyRequest(string Email, string Code, string RegistrationChallengeToken);
