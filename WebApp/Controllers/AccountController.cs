using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace WebApp.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AccountController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Login()
    {
        return RedirectToAction("Cloudflare", new { nextAction = "Login" });
    }

    public IActionResult Register()
    {
        return RedirectToAction("Cloudflare", new { nextAction = "Register" });
    }

    public IActionResult Cloudflare(string nextAction)
    {
        ViewData["NextAction"] = nextAction;
        return View();
    }

    [HttpPost]
    public IActionResult VerifyTurnstile(string nextAction)
    {
        // In a real app, verify the token here.
        
        var properties = new AuthenticationProperties { RedirectUri = "/" };
        if (nextAction == "Register")
        {
            properties.Parameters.Add("prompt", "create");
        }

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    public IActionResult LoginPassword(string token, string email)
    {
        ViewData["Email"] = email;
        return PartialView("_LoginPasswordPartial", token);
    }

    public IActionResult RegisterEnterCode(string token, string email)
    {
        ViewData["Email"] = email;
        return PartialView("_RegisterEnterCodePartial", token);
    }

    public IActionResult RegisterPassword(string token, string email)
    {
        ViewData["Email"] = email;
        return PartialView("_RegisterPasswordPartial", token);
    }

    [HttpPost]
    public async Task<IActionResult> CallApi()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var content = await client.GetStringAsync("https://localhost:5003/api/data");

        return Ok(content);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Console()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        ViewData["AccessToken"] = accessToken;

        return View();
    }

    public IActionResult Logout()
    {
        return SignOut(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }
}
