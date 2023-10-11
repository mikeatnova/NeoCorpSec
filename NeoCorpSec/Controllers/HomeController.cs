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
using System.Net.Http.Headers;

namespace NeoCorpSec.Controllers
{
    [Authorize(Roles = "Neo, SecurityChief, SecurityManager, SecuritySupervisor, SecurityOfficer")]
    public class HomeController : CoreController
    {
        private readonly IConfiguration _configuration;
        private readonly JwtExtractorHelper _jwtExtractorHelper;
        private readonly ILogger<HomeController> _logger;
        private readonly ActivityLogService _activityLogService;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, JwtExtractorHelper jwtExtractorHelper)
        {
            _logger = logger;
            _jwtExtractorHelper = jwtExtractorHelper;
            _configuration = configuration;
            _activityLogService = new ActivityLogService();
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
        public async Task<IActionResult> ActivityLog()
        {
            string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");
            using (var httpClient = InitializeHttpClient())
            {
                // Fetch ActivityLogs
                var activityLogResponse = await httpClient.GetAsync($"{baseUrl}/api/ActivityLog");

                var activityLogOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                List<Models.Reporting.ActivityLog> activityLogs = new List<Models.Reporting.ActivityLog>();

                if (activityLogResponse.IsSuccessStatusCode)
                {
                    // Handle activity log data
                    var activityLogContent = await activityLogResponse.Content.ReadAsStringAsync();
                    activityLogs = JsonSerializer.Deserialize<List<Models.Reporting.ActivityLog>>(activityLogContent, activityLogOptions);
                }

                return View(activityLogs);
            }
        }


        [HttpGet]
        public async Task<IActionResult> CameraList()
        {
            string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");
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
                    ViewBag.LocationMap = locations.ToDictionary(l => l.ID, l => l);

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
                string firstName = claims.FindFirst("FirstName")?.Value;
                string lastName = claims.FindFirst("LastName")?.Value;
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
                        TempData["SuccessMessage"] = $"Note was successfully added to camera #{cameraId}";

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
                                // Prepare the Activity Log with the CurrentActivityLog from CoreController
                                var preparedLog = _activityLogService.PrepareActivityLog(CurrentActivityLog, $"added a note to Camera #{existingCamera.ID}, {existingCamera.Name}");

                                // Log the prepared Activity Log
                                var logContent = new StringContent(JsonConvert.SerializeObject(preparedLog), Encoding.UTF8, "application/json");
                                var logResponse = await httpClient.PostAsync($"{baseUrl}/api/ActivityLog", logContent);

                                if (logResponse.IsSuccessStatusCode)
                                {
                                    TempData["ApiMessage"] = $"Camera '{existingCamera.Name}' was successfully updated.";
                                    return RedirectToAction("CameraList");
                                }
                                else
                                {
                                    var logErrorContent = await logResponse.Content.ReadAsStringAsync();
                                    TempData["ApiLogError"] = $"Activity Log Error: {logResponse.StatusCode}, {logResponse.ReasonPhrase}";
                                    return RedirectToAction("CameraList");
                                }
                            }
                            else
                            {
                                _logger.LogError($"Failed to update ModifiedAt for camera {cameraId}");
                                TempData["ApiLogError"] = $"Failed to update ModifiedAt for camera {cameraId}";
                                return RedirectToAction("CameraList");
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
        public async Task<IActionResult> AddNewCamera(Camera newCamera)
        {
            try
            {
                // Initialize HTTP client and base URL
                string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");
                newCamera.ModifiedAt = DateTime.UtcNow;
                using (var httpClient = InitializeHttpClient())
                {
                    // Serialize the newCamera object to JSON and prepare HTTP content
                    var json = JsonConvert.SerializeObject(newCamera);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Make the POST request
                    var response = await httpClient.PostAsync($"{baseUrl}/api/Camera", content);

                    // Handle response
                    if (response.IsSuccessStatusCode)
                    {
                        // Prepare the Activity Log with the CurrentActivityLog from CoreController
                        var preparedLog = _activityLogService.PrepareActivityLog(CurrentActivityLog, $"created a New Camera and named it {newCamera.Name}.");

                        // Log the prepared Activity Log
                        var logContent = new StringContent(JsonConvert.SerializeObject(preparedLog), Encoding.UTF8, "application/json");
                        var logResponse = await httpClient.PostAsync($"{baseUrl}/api/ActivityLog", logContent);
                        if (logResponse.IsSuccessStatusCode)
                        {
                            TempData["SuccessMessage"] = $"Camera '{newCamera.Name}' has been added successfully.";
                            return RedirectToAction("Admin");
                        }
                        else
                        {
                            var logErrorContent = await logResponse.Content.ReadAsStringAsync();
                            TempData["ApiLogError"] = $"Activity Log Error: {logResponse.StatusCode}, {logResponse.ReasonPhrase}";
                            return RedirectToAction("Admin");
                        }
                    }
                    else
                    {
                        _logger.LogError($"Error adding new camera: {await response.Content.ReadAsStringAsync()}");
                        TempData["ErrorMessage"] = $"Failed to add new camera.";
                        return View("Error", new { message = $"Error: {response.StatusCode}, {response.ReasonPhrase}" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred: {ex.Message}");
                TempData["ErrorMessage"] = $"An exception occurred while adding new camera.";
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

                        // Update the camera's status and ModifiedAt fields
                        existingCamera.CurrentStatus = newStatus;
                        existingCamera.ModifiedAt = DateTime.UtcNow;

                        var updateContent = new StringContent(JsonConvert.SerializeObject(existingCamera), Encoding.UTF8, "application/json");

                        // Update the existing camera
                        var updateResponse = await httpClient.PutAsync($"{baseUrl}/api/Camera/{cameraId}", updateContent);

                        if (updateResponse.IsSuccessStatusCode)
                        {
                            // Prepare the Activity Log with the CurrentActivityLog from CoreController
                            var preparedLog = _activityLogService.PrepareActivityLog(CurrentActivityLog, $"changed the status of Camera #{existingCamera.ID}, {existingCamera.Name }, to {existingCamera.CurrentStatus}.");

                            // Log the prepared Activity Log
                            var logContent = new StringContent(JsonConvert.SerializeObject(preparedLog), Encoding.UTF8, "application/json");
                            var logResponse = await httpClient.PostAsync($"{baseUrl}/api/ActivityLog", logContent);

                            if (logResponse.IsSuccessStatusCode)
                            {
                                TempData["ApiMessage"] = $"Camera '{existingCamera.Name}' was updated successfully";
                                return RedirectToAction("CameraList");
                            }
                            else
                            {
                                var logErrorContent = await logResponse.Content.ReadAsStringAsync();
                                _logger.LogError($"Activity Log Error: {logResponse.StatusCode}, {logResponse.ReasonPhrase}");
                                TempData["ApiLogError"] = $"Activity Log Error: {logResponse.StatusCode}, {logResponse.ReasonPhrase}";
                                return RedirectToAction("CameraList");
                            }
                        }

                        else
                        {
                            var errorContent = await updateResponse.Content.ReadAsStringAsync();
                            TempData["ApiError"] = $"Error: {updateResponse.StatusCode}, {updateResponse.ReasonPhrase}";
                            return View("Error", new { message = $"Error: {updateResponse.StatusCode}, {updateResponse.ReasonPhrase}" });
                        }
                    }
                    else
                    {
                        var errorContent = await fetchResponse.Content.ReadAsStringAsync();
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
            // Initialize variables
            string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");
            var securityUserOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            List<AdminCombinedSecurityUser> securityUsers = new List<AdminCombinedSecurityUser>();
            List<Location> locations = new List<Location>();

            using (var httpClient = InitializeHttpClient())
            {
                // Fetch Security Users and Locations
                var securityUserResponse = await httpClient.GetAsync($"{baseUrl}/api/Auth/get-security-users");
                var locationResponse = await httpClient.GetAsync($"{baseUrl}/api/Location");

                // Handle Location data
                if (locationResponse.IsSuccessStatusCode)
                {
                    var locationContent = await locationResponse.Content.ReadAsStringAsync();
                    locations = JsonSerializer.Deserialize<List<Location>>(locationContent, securityUserOptions);
                }
                ViewBag.Locations = locations;

                // Handle Security User data
                if (securityUserResponse.IsSuccessStatusCode)
                {
                    var securityUserContent = await securityUserResponse.Content.ReadAsStringAsync();
                    securityUsers = JsonSerializer.Deserialize<List<AdminCombinedSecurityUser>>(securityUserContent, securityUserOptions);

                    // Sort by the first role in each user's role list
                    securityUsers = securityUsers.OrderBy(u => u.Roles.FirstOrDefault()).ToList();
                }
            }

            return View("Admin", securityUsers);  // Return the sorted list to the Admin View
        }

        [HttpPost]
        public async Task<IActionResult> AddNewSecurityUser(AdminAddSecurityUser seedUser, string password, string retypePassword)
        {
            if (password != retypePassword)
            {
                TempData["StatusType"] = "danger";
                TempData["StatusMessage"] = $"Passwords do not match, {ViewBag.Username}.";
                return RedirectToAction("Admin");
            }
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

                if (string.IsNullOrEmpty(identityUserId) || string.IsNullOrEmpty(username))
                {
                    return View("Error", new { message = "User identity is not valid" });
                }

                // Initialize HttpClient
                using (var httpClient = InitializeHttpClient())
                {
                    string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");

                    // Log the object being sent
                    _logger.LogInformation($"Sending Security User Object: {JsonSerializer.Serialize(seedUser)}");

                    var json = JsonSerializer.Serialize(seedUser);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync($"{baseUrl}/api/Auth/seed-new-security-user", content);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = $"Security User was successfully created with role {seedUser.Role}";
                        return RedirectToAction("Admin"); // Assuming you have a SecurityUserList action
                    }
                    else
                    {
                        TempData["FailureMessage"] = $"Failed to create Security User with role {seedUser.Role}";
                        return View("Error", new { message = $"Error: {response.StatusCode}, {response.ReasonPhrase}" });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["FailureMessage"] = $"An exception occurred while creating Security User with role {seedUser.Role}";
                return View("Error", new { message = $"An exception occurred: {ex.Message}" });
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

        [HttpPost]
        public async Task<IActionResult> AddNewLocation(Location newLocation)
        {
            try
            {
                // Initialize HTTP client and base URL
                string baseUrl = _configuration.GetValue<string>("NeoNovaApiBaseUrl");
                using (var httpClient = InitializeHttpClient())
                {
                    // Serialize the newLocation object to JSON and prepare HTTP content
                    var json = JsonConvert.SerializeObject(newLocation);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Make the POST request
                    var response = await httpClient.PostAsync($"{baseUrl}/api/Location", content);

                    // Handle response
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = $"Location '{newLocation.Name}' has been added successfully.";
                        return RedirectToAction("Admin");
                    }
                    else
                    {
                        _logger.LogError($"Error adding new location: {await response.Content.ReadAsStringAsync()}");
                        TempData["ErrorMessage"] = $"Failed to add new location.";
                        return View("Error", new { message = $"Error: {response.StatusCode}, {response.ReasonPhrase}" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred: {ex.Message}");
                TempData["ErrorMessage"] = $"An exception occurred while adding new location.";
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