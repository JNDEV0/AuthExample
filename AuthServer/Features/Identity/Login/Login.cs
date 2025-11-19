using Carter;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace AuthServer.Features.Identity.Login;

public class LoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", Handle);
    }

    private async Task<IResult> Handle(LoginRequest request, SignInManager<Data.ApplicationUser> signInManager, IDataProtectionProvider protectionProvider, HttpContext context)
    {
        var protector = protectionProvider.CreateProtector("Authentication.Login.Challenge:" + request.Email);
        try
        {
            var unprotectedToken = protector.Unprotect(request.ChallengeToken);
            var creationTime = DateTime.Parse(unprotectedToken);

            if (DateTime.UtcNow - creationTime > TimeSpan.FromMinutes(5))
            {
                return Results.BadRequest("Challenge token expired.");
            }
        }
        catch
        {
            return Results.BadRequest("Invalid challenge token.");
        }

        var result = await signInManager.CheckPasswordSignInAsync(await signInManager.UserManager.FindByEmailAsync(request.Email), request.Password, false);

        if (result.Succeeded)
        {
            var user = await signInManager.UserManager.FindByEmailAsync(request.Email);
            await signInManager.SignInAsync(user, false);
            // context.Response.Headers["HX-Redirect"] = "/connect/authorize";
            return Results.Ok(new { success = true, redirectUrl = "/connect/authorize" });
        }

        return Results.Unauthorized();
    }
}

public record LoginRequest(string Email, string Password, string ChallengeToken);
