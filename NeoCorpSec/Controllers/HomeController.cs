using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoCorpSec.Models;
using Microsoft.AspNetCore.JsonPatch;
using NeoCorpSec.Models.Reporting;
using NeoCorpSec.Services;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using NeoCorpSec.Models.CameraManagement;
using NeoCorpSec.Models.Authenitcation;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
                {9, Tuple.Create("Woodbury", "New Jersey")},
                {13, Tuple.Create("Polaris", "Andromeda")}
            };
            ViewBag.LocationMap = locationMap;
            using (var httpClient = InitializeHttpClient())
            {
                // Fetch Cameras
                var cameraResponse = await httpClient.GetAsync($"{baseUrl}/api/Camera");
                // Fetch Locations
                var locationResponse = await httpClient.GetAsync($"{baseUrl}/api/Location");
                // Fetch Locations
                var noteResponse = await httpClient.GetAsync($"{baseUrl}/api/Note");

                var cameraOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var locationOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var noteOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                List<Models.CameraManagement.Camera> cameras = new List<Models.CameraManagement.Camera>();
                List<Models.CameraManagement.Location> locations = new List<Models.CameraManagement.Location>();
                List<Models.Reporting.Note> notes = new List<Models.Reporting.Note>();

                if (cameraResponse.IsSuccessStatusCode)
                {
                    // Handle camera data
                    var cameraContent = await cameraResponse.Content.ReadAsStringAsync();
                    cameras = JsonSerializer.Deserialize<List<Models.CameraManagement.Camera>>(cameraContent, cameraOptions);
                }

                if (locationResponse.IsSuccessStatusCode)
                {
                    // Handle location data
                    var locationContent = await locationResponse.Content.ReadAsStringAsync();
                    locations = JsonSerializer.Deserialize<List<Models.CameraManagement.Location>>(locationContent, locationOptions);
                }

                if (noteResponse.IsSuccessStatusCode)
                {
                    // Handle camera note data
                    var noteContent = await noteResponse.Content.ReadAsStringAsync();
                    notes = JsonSerializer.Deserialize<List<Models.Reporting.Note>>(noteContent, noteOptions);
                }

                if (cameraResponse.IsSuccessStatusCode && locationResponse.IsSuccessStatusCode && noteResponse.IsSuccessStatusCode)
                {
                    // Map LocationId to Location objects
                    ViewBag.LocationMap = locations.ToDictionary(l => l.ID); // O(N)

                    // Group notes by CameraId
                    var cameraSpecificNotes = notes.Where(n => n.NoteableType == "Camera")
                                .GroupBy(n => n.NoteableId)
                                .ToDictionary(g => g.Key, g => g.ToList()); // O(N)
                    ViewBag.CameraNotes = cameraSpecificNotes;

                    var cameraStatusCounts = cameras.GroupBy(c => c.LocationId)
                                            .ToDictionary(g => g.Key,
                                                          g => g.GroupBy(c => c.CurrentStatus)
                                                                .ToDictionary(s => s.Key, s => s.Count())); // O(N)

                    ViewBag.CameraStatusCounts = cameraStatusCounts;

                    return View(cameras);
                }
            }
            return View(new List<Models.CameraManagement.Camera>());
        }

        [HttpPost]
        public async Task<IActionResult> AddNoteToCamera(int cameraId, string newNote, string noteableType)
        {
            try
            {
                // Extract user claims
                var claims = _jwtExtractorHelper.GetClaimsFromJwt();
                if (claims == null)
                {
                    return View("Error", new { message = "Claims are null" });
                }

                string identityUserId = claims?.FindFirst("sub")?.Value ?? string.Empty;
                string username = claims.FindFirst(ClaimTypes.Name)?.Value;
                string firstName = claims.FindFirst(ClaimTypes.GivenName)?.Value;
                string lastName = claims.FindFirst(ClaimTypes.Surname)?.Value;
                string role = claims.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(identityUserId) || string.IsNullOrEmpty(username))
                {
                    return View("Error", new { message = "User identity is not valid" });
                }

                // Create Note object
                var newNoteObj = new Note
                {
                    UserId = identityUserId,
                    Content = newNote,
                    Username = username,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                    NoteableType = noteableType,
                    NoteableId = cameraId,
                };

                // Initialize HttpClient
                using (var httpClient = InitializeHttpClient())
                {
                    string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");

                    // Log the object being sent
                    Console.WriteLine($"Sending Note Object: {JsonSerializer.Serialize(newNoteObj)}");

                    var json = JsonSerializer.Serialize(newNoteObj);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync($"{baseUrl}/api/Note", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Success case for adding note
                        TempData["SuccessMessage"] = $"Note was successfully added to camera {cameraId}";

                        // Fetch the existing camera to update ModifiedAt
                        var fetchCameraResponse = await httpClient.GetAsync($"{baseUrl}/api/Camera/{cameraId}");
                        if (fetchCameraResponse.IsSuccessStatusCode)
                        {
                            var cameraJson = await fetchCameraResponse.Content.ReadAsStringAsync();
                            var existingCamera = JsonConvert.DeserializeObject<Camera>(cameraJson);

                            // Update ModifiedAt
                            existingCamera.ModifiedAt = DateTime.UtcNow;

                            // Serialize updated camera
                            var updateCameraContent = new StringContent(JsonConvert.SerializeObject(existingCamera), Encoding.UTF8, "application/json");

                            // Send PUT request to update camera
                            var updateCameraResponse = await httpClient.PutAsync($"{baseUrl}/api/Camera/{cameraId}", updateCameraContent);

                            if (updateCameraResponse.IsSuccessStatusCode)
                            {
                                TempData["UpdateMessage"] = $"Camera {cameraId} ModifiedAt was successfully updated.";
                            }
                            else
                            {
                                TempData["FailureMessage"] = $"Failed to update ModifiedAt for camera {cameraId}";
                            }
                        }
                        else
                        {
                            TempData["FailureMessage"] = $"Failed to fetch camera {cameraId} for updating ModifiedAt";
                        }

                        return RedirectToAction("CameraList");
                    }
                    else
                    {
                        TempData["FailureMessage"] = $"Failed to add note to camera {cameraId}";
                        return View("Error", new { message = $"Error: {response.StatusCode}, {response.ReasonPhrase}" });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["FailureMessage"] = $"An exception occurred while adding note to camera {cameraId}";
                return View("Error", new { message = $"An exception occurred: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> EditCameraCurrentStatus(int cameraId, string newStatus)
        {
            try
            {
                using (var httpClient = InitializeHttpClient())
                {
                    string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");

                    // Fetch the existing camera
                    var fetchResponse = await httpClient.GetAsync($"{baseUrl}/api/Camera/{cameraId}");

                    if (fetchResponse.IsSuccessStatusCode)
                    {
                        var cameraJson = await fetchResponse.Content.ReadAsStringAsync();
                        var existingCamera = JsonConvert.DeserializeObject<Camera>(cameraJson);

                        // Update the fields you want to change
                        existingCamera.CurrentStatus = newStatus;
                        existingCamera.ModifiedAt = DateTime.UtcNow;

                        // Serialize it
                        var updateContent = new StringContent(JsonConvert.SerializeObject(existingCamera), Encoding.UTF8, "application/json");

                        // Send the PUT request
                        var updateResponse = await httpClient.PutAsync($"{baseUrl}/api/Camera/{cameraId}", updateContent);

                        if (updateResponse.IsSuccessStatusCode)
                        {
                            // Success case
                            TempData["ApiMessage"] = $"Camera '{existingCamera.Name}' was updated successfully";
                            return RedirectToAction("CameraList");
                        }
                        else
                        {
                            // Handle error
                            var errorContent = await updateResponse.Content.ReadAsStringAsync();
                            _logger.LogError($"Error updating camera: {errorContent}");
                            TempData["ApiError"] = $"Error: {updateResponse.StatusCode}, {updateResponse.ReasonPhrase}";
                            return View("Error", new { message = $"Error: {updateResponse.StatusCode}, {updateResponse.ReasonPhrase}" });
                        }
                    }
                    else
                    {
                        // Handle error
                        var errorContent = await fetchResponse.Content.ReadAsStringAsync();
                        _logger.LogError($"Error fetching camera: {errorContent}");
                        TempData["ApiError"] = $"Error: {fetchResponse.StatusCode}, {fetchResponse.ReasonPhrase}";
                        return View("Error", new { message = $"Error: {fetchResponse.StatusCode}, {fetchResponse.ReasonPhrase}" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred: {ex.Message}");
                TempData["ApiError"] = $"An exception occurred: {ex.Message}";
                return View("Error", new { message = $"An exception occurred: {ex.Message}" });
            }
        }



        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult CameraHistory()
        {
            return View();
        }

        public async Task<IActionResult> Admin()
        {
            string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");
            using (var httpClient = InitializeHttpClient())
            {
                // Fetch Security Users
                var securityUserResponse = await httpClient.GetAsync($"{baseUrl}/api/Auth/get-security-users");

                var securityUserOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                List<AdminCombinedSecurityUser> securityUsers = new List<AdminCombinedSecurityUser>();

                if (securityUserResponse.IsSuccessStatusCode)
                {
                    // Handle Security User data
                    var securityUserContent = await securityUserResponse.Content.ReadAsStringAsync();
                    securityUsers = JsonSerializer.Deserialize<List<AdminCombinedSecurityUser>>(securityUserContent, securityUserOptions);

                    // Sort the list by the first role in each user's role list
                    securityUsers = securityUsers.OrderBy(u => u.Roles.FirstOrDefault()).ToList();
                }

                return View("Admin", securityUsers); // Return the sorted list to the Admin View
            }
        }

        [HttpPost]
        public async Task<IActionResult> AdminUpdateSecurityUser(UpdateSecurityUserDto updatedUser)
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
                        TempData["SuccessMessage"] = $"{updatedUser.UserName} has been updated successfully.";
                        return RedirectToAction("Admin");
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