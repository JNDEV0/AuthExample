using AspNet.Security.OAuth.GitHub;
using AuthServer.Data;
using AuthServer.Middleware;
using Carter;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quartz;
using AuthServer;
using OpenIddict.Server;
using StackExchange.Redis;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// CRITICAL: Enable legacy timestamp behavior.
// This creates a compatibility bridge for the existing domain model, preventing runtime
// InvalidCastExceptions regarding DateTime.Kind.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // The Agent replaces the SQLite configuration with Npgsql.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
      
    options.UseNpgsql(connectionString, npgsqlOptions => {
        // Resiliency Strategy: Enable retry logic.
        // This is essential for cloud-native apps connecting to Aurora Serverless,
        // which may experience transient connection drops during auto-scaling events.
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });

    options.UseOpenIddict();
});

// Retrieve the Redis connection string.
// In K8s, this will be injected via Environment Variable from the Secret Store CSI Driver.
// The connection string format for ElastiCache typically includes the endpoint and SSL.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") 
                           ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

if (!string.IsNullOrEmpty(redisConnectionString))
{
    // Establish the connection to ElastiCache
    var redis = ConnectionMultiplexer.Connect(redisConnectionString);
      
    builder.Services.AddDataProtection()
        // Persist keys to a specific Redis key. 
        // This acts as the shared repository for the XML key ring.
       .PersistKeysToStackExchangeRedis(redis, "Net8IdentityEco-DataProtection-Keys")
          
        // CRITICAL: Set a unified Application Name.
        // This ensures that the key derivation logic is identical across both microservices,
        // allowing them to share cookies and tokens securely.
       .SetApplicationName("Net8IdentityEco"); 
}
else
{
    // Fallback for local development (SQLite/Localhost)
    builder.Services.AddDataProtection()
       .SetApplicationName("Net8IdentityEco");
}

// Register the Identity services.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    })
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
    });


// OpenIddict offers native integration with Quartz.NET to perform scheduled tasks
// (like pruning orphaned tokens from the database) at regular intervals.
builder.Services.AddQuartz(options =>
{
    options.UseMicrosoftDependencyInjectionJobFactory();
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});

// Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
builder.Services.AddHostedService<Worker>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.IsEssential = true;
});

builder.Services.AddOpenIddict()
    // Register the OpenIddict core components.
    .AddCore(options =>
    {
        // Configure OpenIddict to use the Entity Framework Core stores and models.
        // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();

        // Enable Quartz.NET integration.
        options.UseQuartz();
    })
    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Enable the authorization, logout, token and userinfo endpoints.
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetLogoutEndpointUris("connect/logout")
               .SetTokenEndpointUris("connect/token")
               .SetUserinfoEndpointUris("connect/userinfo")
                .SetIntrospectionEndpointUris("connect/introspect");

        // Mark the "email", "profile" and "roles" scopes as supported scopes.
        options.RegisterScopes("email", "profile", "roles", "resource_api");

        // Note: this sample only uses the authorization code flow but you can enable
        // the other flows if you need to.
        options.AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow();
        
        options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core options.
        options.UseAspNetCore()
               .EnableStatusCodePagesIntegration()
               .EnableAuthorizationEndpointPassthrough()
               .EnableLogoutEndpointPassthrough();

        options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.ProcessSignInContext>(builder =>
        {
            builder.UseInlineHandler(context =>
            {
                foreach (var claim in context.Principal.Claims)
                {
                    logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
                }
                return default;
            });
        });
    })
    // Register the OpenIddict validation components.
    .AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization();



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Added Razor Pages services

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebApp", policy =>
    {
        policy.WithOrigins("https://localhost:5002")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("WebApp");
app.UseStaticFiles(); // Added for static assets

app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();
app.MapControllers();
app.MapRazorPages(); // Added Razor Pages mapping

app.MapGet("/test", () => "Hello World!");

app.Run();