using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoCorpSec.Models.Authenitcation;

namespace NeoCorpSec.Controllers
{
    public class SecurityUserController : CoreController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SecurityUserController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : base()
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();  // This returns the Login.cshtml view
        }

        [Authorize(Roles = "Neo, SecurityChief, SecurityManager, SecuritySupervisor, SecurityOfficer")]
        public IActionResult Profile()
        {
            return View();  // This returns the Login.cshtml view
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginPost(LoginSecurityUser loginSecurityUser)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("apiClient");
                var response = await httpClient.PostAsJsonAsync("/api/auth/login", loginSecurityUser); // Replace the endpoint as needed

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
                    if (result?.Token != null)
                    {
                        HttpContext.Response.Cookies.Append("NeoWebAppCookie", result.Token, new CookieOptions
                        {
                            SameSite = SameSiteMode.None,
                            HttpOnly = true,
                            Secure = true
                        });
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        TempData["ApiError"] = "Invalid credentials.";
                        return RedirectToAction("Login", "SecurityUser");
                    }
                }
                TempData["ApiError"] = "Invalid credentials.";
                return RedirectToAction("Login", "SecurityUser"); // Replace with the appropriate account/login view
            }
            catch (HttpRequestException ex)
            {
                // Redirect or show error as you see fit
                TempData["ApiError"] = "Could not connect to the API. Please try again later.";
                return RedirectToAction("Login", "SecurityUser"); // Replace with the appropriate account/login view
            }
        }

        // LOGOUT
        [Authorize(Roles = "Neo, SecurityChief, SecurityManager, SecuritySupervisor, SecurityOfficer")]
        public IActionResult Logout()
        {
            // Delete the authentication cookie
            Response.Cookies.Delete("NeoWebAppCookie");

            // Redirect to the login page
            return RedirectToAction("Login", "SecurityUser");
        }
    }
}
