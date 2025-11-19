using Carter;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthServer.Features.Identity.ExternalLogin;

public class ExternalLoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/external-login", (string provider, [FromQuery] string returnUrl, SignInManager<Data.ApplicationUser> signInManager) =>
        {
            var redirectUrl = $"/api/auth/external-login-callback?returnUrl={returnUrl}";
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Results.Challenge(properties, new[] { provider });
        })
        .WithTags("Identity");

        app.MapGet("/api/auth/external-login-callback", async ([FromQuery] string returnUrl, SignInManager<Data.ApplicationUser> signInManager, UserManager<Data.ApplicationUser> userManager) =>
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return Results.BadRequest();
            }

            var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return Results.Redirect(returnUrl);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email != null)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new Data.ApplicationUser { UserName = email, Email = email };
                    await userManager.CreateAsync(user);
                }
                await userManager.AddLoginAsync(user, info);
                await signInManager.SignInAsync(user, isPersistent: false);
                return Results.Redirect(returnUrl);
            }

            return Results.BadRequest();
        })
        .WithTags("Identity");
    }
}
