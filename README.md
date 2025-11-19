# AuthExample

A .NET 8 authentication solution demonstrating OpenIddict, ASP.NET Core Identity, and modern UI patterns.

## Architecture

This solution consists of three projects:

### AuthServer (localhost:5001)
- **Identity Provider** using OpenIddict and ASP.NET Core Identity
- Multi-stage login and registration flows
- Cloudflare Turnstile integration (simulated)
- Tailwind CSS + Lucide icons UI
- SQLite database for user storage
- Razor Pages for authentication UI

### WebApp (localhost:5002)
- **Client Application** using OpenID Connect
- MVC architecture with HTMX
- Home page with Cloudflare verification flow
- Redirects to AuthServer for authentication

### ResourceApi (localhost:5003)
- **Protected API** using JWT Bearer authentication
- Minimal API with Carter
- Secured endpoints requiring valid access tokens

## Prerequisites

- .NET 8 SDK
- SQLite

## Getting Started

1. Clone the repository
2. Run the AuthServer:
   ```bash
   cd AuthServer
   dotnet run --urls=https://localhost:5001
   ```
3. Run the ResourceApi:
   ```bash
   cd ResourceApi
   dotnet run --urls=https://localhost:5003
   ```
4. Run the WebApp:
   ```bash
   cd WebApp
   dotnet run --urls=https://localhost:5002
   ```

## Usage

1. Navigate to https://localhost:5002
2. Click "Sign In" or "Register"
3. Complete the Cloudflare verification (auto-simulated)
4. Authenticate via the AuthServer
5. Return to WebApp Console

## Flow

```
WebApp (5002) → Cloudflare Check → AuthServer (5001) → Login/Register → Console
```

## Technologies

- ASP.NET Core 8
- OpenIddict
- ASP.NET Core Identity
- Entity Framework Core (SQLite)
- Quartz.NET
- Carter (Minimal APIs)
- Tailwind CSS
- HTMX
