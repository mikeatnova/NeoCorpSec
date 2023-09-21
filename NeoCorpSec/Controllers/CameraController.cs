using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NeoCorpSec.Services;
using System.Text;
using System.Text.Json;

namespace NeoCorpSec.Controllers
{
    [Authorize(Roles = "Neo, SecurityChief, SecurityManager, SecuritySupervisor, SecurityOfficer")]
    public class CameraController : CoreController
    {
        private readonly IConfiguration _configuration;
        private readonly JwtExtractorHelper _jwtExtractorHelper;

        public CameraController(IConfiguration configuration, JwtExtractorHelper jwtExtractorHelper)
            : base() // Call the base constructor with the required parameter
        {
            _jwtExtractorHelper = jwtExtractorHelper;
            _configuration = configuration;
        }

        private async Task<IActionResult> GetViewAsync<T>(string url)
        {
            using (var httpClient = InitializeHttpClient())
            {
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var items = JsonSerializer.Deserialize<List<T>>(content, options);
                    return View(items);
                }
            }
            return View(new List<T>()); // Return an empty list if the request fails
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
