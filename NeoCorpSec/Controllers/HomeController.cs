using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoCorpSec.Models;
using NeoCorpSec.Services;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace NeoCorpSec.Controllers
{
    [Authorize(Roles = "Neo, SecurityChief, SecurityManager, SecuritySupervisor, SecurityOfficer")]
    public class HomeController : CoreController
    {
        private readonly IConfiguration _configuration;
        private readonly JwtExtractorHelper _jwtExtractorHelper;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, JwtExtractorHelper jwtExtractorHelper)
        {
            _logger = logger;
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
            return View(new List<T>());
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult TourPage()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CameraList()
        {
            string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");
            Dictionary<int, Tuple<string, string>> locationMap = new Dictionary<int, Tuple<string, string>>()
            {
                {1, Tuple.Create("Attleboro", "Massachusetts")},
                {2, Tuple.Create("Framingham", "Massachusetts")},
                {3, Tuple.Create("Sheffield", "Massachusetts")},
                {4, Tuple.Create("Dracut", "Massachusetts")},
                {5, Tuple.Create("Pawtucket", "Rhode Island")},
                {6, Tuple.Create("Central Falls", "Rhode Island")},
                {7, Tuple.Create("Thorndike", "Maine")},
                {8, Tuple.Create("Greenville Junction", "Maine")},
                {9, Tuple.Create("Woodbury", "New Jersey")}
            };
            ViewBag.LocationMap = locationMap;
            using (var httpClient = InitializeHttpClient())
            {
                // Fetch Cameras
                var cameraResponse = await httpClient.GetAsync($"{baseUrl}/api/Camera");

                // Fetch Locations
                var locationResponse = await httpClient.GetAsync($"{baseUrl}/api/Location");

                if (cameraResponse.IsSuccessStatusCode && locationResponse.IsSuccessStatusCode)
                {
                    var cameraContent = await cameraResponse.Content.ReadAsStringAsync();
                    var locationContent = await locationResponse.Content.ReadAsStringAsync();

                    var cameraOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var locationOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                    var cameras = JsonSerializer.Deserialize<List<Models.CameraManagment.Camera>>(cameraContent, cameraOptions);
                    var locations = JsonSerializer.Deserialize<List<Models.CameraManagment.Location>>(locationContent, locationOptions);

                    // Map LocationId to Location objects
                    ViewBag.LocationMap = locations.ToDictionary(l => l.ID); // O(N)


                    return View(cameras);
                }
            }
            return View(new List<Models.CameraManagment.Camera>());
        }

        [HttpPost]
        public async Task<IActionResult> PutCamera(int id, Models.CameraManagment.Camera camera)
        {
            using (var httpClient = InitializeHttpClient())
            {
                string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");
                camera.ModifiedAt = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(camera);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PutAsync($"{baseUrl}/api/Camera/{id}", content);
                if (response.IsSuccessStatusCode)
                {
                    // Redirect to the GET action to refresh the page
                    return RedirectToAction("CameraList");
                }
            }
            // Handle error, e.g., return a view with an error message
            return View("Error");
        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult CameraHistory()
        {
            return View();
        }

        public IActionResult Admin()
        {
            return View();
        }
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}