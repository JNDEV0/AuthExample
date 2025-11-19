using Carter;

namespace AuthServer.Features.MockAuth;

public class MockAuthEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/mock-auth", () =>
        {
            var html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Mock Consent</title>
                </head>
                <body>
                    <h1>Mock Consent Screen</h1>
                    <p>Do you consent to grant access to your account?</p>
                    <button onclick=""accept()"">Accept</button>
                    <button onclick=""deny()"">Deny</button>
                    <script>
                        function accept() {
                            window.opener.postMessage('Success', '*');
                            window.close();
                        }

                        function deny() {
                            window.close();
                        }
                    </script>
                </body>
                </html>";
            return Results.Content(html, "text/html");
        })
        .WithTags("Identity");
    }
}
