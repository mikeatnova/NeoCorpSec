using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoCorpSec.Models.Authenitcation;
using NeoCorpSec.Models.Messaging;
using NeoCorpSec.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace NeoCorpSec.Controllers
{
    public class SecurityUserController : CoreController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly JwtExtractorHelper _jwtExtractorHelper;


        public SecurityUserController(IHttpClientFactory httpClientFactory, IConfiguration configuration, JwtExtractorHelper jwtExtractorHelper) : base()
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _jwtExtractorHelper = jwtExtractorHelper;
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
        public async Task<IActionResult> SendPalantirMessage(PalantirMessage newMessage)
        {
            try
            {
                // Extract user claims
                var claims = _jwtExtractorHelper.GetClaimsFromJwt();
                if (claims == null)
                {
                    TempData["ErrorMessage"] = "Claims are null.";
                    return RedirectToAction("Profile");
                }
                string username = claims.FindFirst(ClaimTypes.Name)?.Value;

                // Set Realm and Username
                newMessage.Realm = "CorpSec";
                newMessage.Status = "Unread";
                newMessage.Username = username;

                // Initialize HttpClient
                using (var httpClient = InitializeHttpClient())
                {
                    string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");

                    // Serialize the newMessage object to JSON and prepare HTTP content
                    var json = JsonSerializer.Serialize(newMessage);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Make the POST request
                    var response = await httpClient.PostAsync($"{baseUrl}/api/PalantirMessages", content);

                    // Handle response
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Palantir message sent successfully.";
                        return RedirectToAction("Profile");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Error: {response.StatusCode}, {response.ReasonPhrase}";
                        return RedirectToAction("Profile");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An exception occurred: {ex.Message}";
                return RedirectToAction("Profile");
            }
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
