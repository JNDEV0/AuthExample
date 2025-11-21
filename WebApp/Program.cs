using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using StackExchange.Redis;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

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

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = "https://localhost:5001";
    options.ClientId = "web-app";
    options.ClientSecret = "super-secret";
    options.ResponseType = "code";
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("resource_api");
    options.SaveTokens = true;
    options.TokenValidationParameters.NameClaimType = "name";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
