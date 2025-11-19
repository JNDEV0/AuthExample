using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace AuthServer.Middleware
{
    public class TurnstileMiddleware
    {
        private readonly RequestDelegate _next;

        public TurnstileMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.Equals("/connect/authorize", StringComparison.OrdinalIgnoreCase) &&
                !context.Session.Keys.Contains("CloudflareVerified"))
            {
                var returnUrl = context.Request.Path + context.Request.QueryString;
                context.Response.Redirect($"/Cloudflare?returnUrl={Uri.EscapeDataString(returnUrl)}");
                return;
            }

            await _next(context);
        }
    }
}
