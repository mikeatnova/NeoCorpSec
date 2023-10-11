using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoCorpSec.Models.Authenitcation;
using NeoCorpSec.Models.Messaging;
using NeoCorpSec.Services;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NeoCorpSec.Controllers
{
    public class SecurityUserController : CoreController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
        private readonly JwtExtractorHelper _jwtExtractorHelper;
        private readonly ActivityLogService _activityLogService;

        public SecurityUserController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration, JwtExtractorHelper jwtExtractorHelper) : base()
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _jwtExtractorHelper = jwtExtractorHelper;
            _activityLogService = new ActivityLogService();
            _logger = logger;
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
        public async Task<IActionResult> Profile()
        {
            // Initialize variables
            string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");
            var securityUserOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            AdminCombinedSecurityUser securityUser = new AdminCombinedSecurityUser();

            using (var httpClient = InitializeHttpClient())
            {
                // Fetch the specific Security User by UserId
                var userId = ViewBag.UserId;  // Assuming you've set this in the CoreController
                var securityUserResponse = await httpClient.GetAsync($"{baseUrl}/api/Auth/get-security-user/{userId}");

                // Handle Security User data
                if (securityUserResponse.IsSuccessStatusCode)
                {
                    var securityUserContent = await securityUserResponse.Content.ReadAsStringAsync();
                    securityUser = JsonSerializer.Deserialize<AdminCombinedSecurityUser>(securityUserContent, securityUserOptions);
                }
            }

            ViewBag.SecurityUser = securityUser;
            PalantirMessage palantirMessage = new PalantirMessage();
            ViewBag.PalantirMessage = palantirMessage;

            return View("Profile", securityUser);  // This returns the Profile view
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSecurityUser(UpdateSecurityUserDto updatedUser)
        {
            try
            {
                // Initialize HTTP client and base URL
                string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");
                using (var httpClient = InitializeHttpClient())
                {
                    // Serialize the updatedUser object to JSON and prepare HTTP content
                    var json = JsonConvert.SerializeObject(updatedUser);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Make the PUT request
                    var response = await httpClient.PutAsync($"{baseUrl}/api/Auth/update-security-user", content);

                    // Handle response
                    if (response.IsSuccessStatusCode)
                    {
                        // Prepare the Activity Log with the CurrentActivityLog from CoreController
                        var preparedLog = _activityLogService.PrepareActivityLog(CurrentActivityLog, $"updated their own details.", "Update");

                        // Log the prepared Activity Log
                        var logContent = new StringContent(JsonConvert.SerializeObject(preparedLog), Encoding.UTF8, "application/json");
                        var logResponse = await httpClient.PostAsync($"{baseUrl}/api/ActivityLog", logContent);
                        if (logResponse.IsSuccessStatusCode)
                        {
                            TempData["SuccessMessage"] = $"{updatedUser.UserName} has been updated successfully.";
                            return RedirectToAction("Profile");
                        }
                        else
                        {
                            var logErrorContent = await logResponse.Content.ReadAsStringAsync();
                            TempData["ApiLogError"] = $"Activity Log Error: {logResponse.StatusCode}, {logResponse.ReasonPhrase}";
                            return RedirectToAction("Profile");
                        }
                    }
                    else
                    {
                        _logger.LogError($"Error updating security user: {await response.Content.ReadAsStringAsync()}");
                        TempData["ErrorMessage"] = $"Failed to update {updatedUser.UserName}.";
                        return View("Error", new { message = $"Error: {response.StatusCode}, {response.ReasonPhrase}" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred: {ex.Message}");
                TempData["ErrorMessage"] = $"An exception occurred while updating {updatedUser.UserName}.";
                return View("Error", new { message = $"An exception occurred: {ex.Message}" });
            }
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
