using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DemoWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace DemoWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly IDistributedCache _cache;

        public AuthController(IDistributedCache cache)
        {
            _cache = cache;
        }
        public IActionResult Index()
        {
            return Content("OK");
        }

        void CreateChallenge()
        {
            var token = Guid.NewGuid().ToString();
            var rnd = new Random();
            var a = rnd.Next(9) + 1;
            var b = rnd.Next(9) + 1;
            var answer = a + b;
            var challenge = $"{a}+{b}=";
            _cache.SetString(token, answer.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            ViewBag.Challenge = challenge;
            ViewBag.Token = token;
        }

        [HttpGet]
        public IActionResult Login(string ReturnUrl)
        {
            CreateChallenge();
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Login(string token, string name, string response, string ReturnUrl)
        {
            var message = string.Empty;
            try
            {
                var answer = await _cache.GetStringAsync(token);
                if (answer == null)
                    throw new ApplicationException("Invalid Token");
                await _cache.RemoveAsync(token);
                if (answer == response)
                {
                    await SignIn(name);
                    return Redirect(ReturnUrl ?? "~/");
                }
                else
                {
                    message = "Login Failed";
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            ViewBag.Message = message;
            ViewBag.Name = name;
            CreateChallenge();
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("~/");
        }

        async Task SignIn(string name)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, name),
                new Claim("FullName", name),
                new Claim(ClaimTypes.Role, "Administrator"),
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                //AllowRefresh = <bool>,
                // Refreshing the authentication session should be allowed.

                //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                // The time at which the authentication ticket expires. A 
                // value set here overrides the ExpireTimeSpan option of 
                // CookieAuthenticationOptions set with AddCookie.

                //IsPersistent = true,
                // Whether the authentication session is persisted across 
                // multiple requests. When used with cookies, controls
                // whether the cookie's lifetime is absolute (matching the
                // lifetime of the authentication ticket) or session-based.

                //IssuedUtc = <DateTimeOffset>,
                // The time at which the authentication ticket was issued.

                //RedirectUri = <string>
                // The full path or absolute URI to be used as an http 
                // redirect response value.
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

        }
    }
}
