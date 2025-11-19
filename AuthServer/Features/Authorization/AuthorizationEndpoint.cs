using System.Security.Claims;
using Carter;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthServer.Features.Authorization;

public class AuthorizationEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/connect/authorize", HandleAsync);
        app.MapPost("/connect/authorize", HandleAsync); // Support POST for form_post response mode if needed
    }

    private async Task<IResult> HandleAsync(HttpContext context, IOpenIddictApplicationManager applicationManager)
    {
        var request = context.GetOpenIddictServerRequest();
        if (request == null)
        {
            return Results.BadRequest("Unable to retrieve the OpenID Connect request.");
        }

        // Retrieve the user principal stored in the authentication cookie.
        var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!result.Succeeded)
        {
            // If the user is not authenticated, challenge the authentication middleware
            // to redirect the user to the login page.
            return Results.Challenge(properties: null, authenticationSchemes: [IdentityConstants.ApplicationScheme]);
        }

        var user = result.Principal;

        // Create a new ClaimsPrincipal containing the claims that
        // will be used to create an id_token, a token or a code.
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        // Add the claims that will be persisted in the tokens.
        identity.AddClaim(OpenIddictConstants.Claims.Subject, user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        identity.AddClaim(OpenIddictConstants.Claims.Email, user.FindFirst(ClaimTypes.Email)!.Value);
        identity.AddClaim(OpenIddictConstants.Claims.Name, user.FindFirst(ClaimTypes.Name)!.Value);

        // Add destination claims
        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
        }

        var principal = new ClaimsPrincipal(identity);

        // Set the scopes
        principal.SetScopes(request.GetScopes());

        // Signing in with the OpenIddict authentication scheme triggers OpenIddict to issue a code/token.
        return Results.SignIn(principal, properties: null, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
