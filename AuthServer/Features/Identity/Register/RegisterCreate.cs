using Carter;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace AuthServer.Features.Identity.Register;

public class RegisterCreateEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register-create", Handle);
    }

    private async Task<IResult> Handle(RegisterCreateRequest request, UserManager<Data.ApplicationUser> userManager, SignInManager<Data.ApplicationUser> signInManager, IDataProtectionProvider protectionProvider)
    {
        var protector = protectionProvider.CreateProtector("Authentication.Register.PasswordChallenge:" + request.Email);
        try
        {
            var unprotectedToken = protector.Unprotect(request.PasswordChallengeToken);
            var creationTime = DateTime.Parse(unprotectedToken);

            if (DateTime.UtcNow - creationTime > TimeSpan.FromMinutes(5))
            {
                return Results.BadRequest("Password challenge token expired.");
            }
        }
        catch
        {
            return Results.BadRequest("Invalid password challenge token.");
        }

        var user = new Data.ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            await signInManager.SignInAsync(user, isPersistent: false);
            return Results.Ok(new { RedirectUrl = "/connect/authorize" });
        }

        return Results.BadRequest(result.Errors);
    }
}

public record RegisterCreateRequest(string Email, string Password, string ConfirmPassword, string PasswordChallengeToken);
