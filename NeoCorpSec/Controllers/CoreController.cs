using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using NeoCorpSec.Models.Reporting;

namespace NeoCorpSec.Controllers
{
    public class CoreController : Controller
    {
        public ActivityLog CurrentActivityLog { get; set; } = new ActivityLog();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var claims = HttpContext.Items["UserClaims"] as ClaimsPrincipal;
            if (claims != null)
            {
                ViewBag.Role = claims.FindFirst(ClaimTypes.Role)?.Value;
                ViewBag.Username = claims.FindFirst(ClaimTypes.Name)?.Value;
                ViewBag.Email = claims.FindFirst(ClaimTypes.Email)?.Value;
                ViewBag.UserId = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                ViewBag.FirstName = claims.FindFirst("FirstName")?.Value;
                ViewBag.LastName = claims.FindFirst("LastName")?.Value;

                // Populate ActivityLog fields
                CurrentActivityLog.IdentityUserId = ViewBag.UserId;
                CurrentActivityLog.UserName = ViewBag.Username;
                CurrentActivityLog.UserRole = ViewBag.Role;
                CurrentActivityLog.FirstName = ViewBag.FirstName;
                CurrentActivityLog.LastName = ViewBag.LastName;
                // You can add additional fields here if your JWT contains them, like FirstName and LastName
            }

            bool isAuthenticated = IsUserAuthenticated();
            ViewBag.IsUserAuthenticated = isAuthenticated;

            if (!isAuthenticated)
            {
                // Invalidate the cookie here
                HttpContext.Response.Cookies.Delete("NeoWebAppCookie");
            }

            base.OnActionExecuting(context);
        }

        public bool IsUserAuthenticated()
        {
            var claims = HttpContext.Items["UserClaims"] as ClaimsPrincipal;
            if (claims != null)
            {
                var usernameClaim = claims.FindFirst(ClaimTypes.Name);
                if (usernameClaim != null)
                {
                    ViewBag.UserNameOrEmail = usernameClaim.Value;
                }
                return true;
            }
            return false;
        }

        // Common method to initialize HttpClient with authorization header
        public HttpClient InitializeHttpClient()
        {
            var httpClient = new HttpClient();

            // Retrieve the token from the cookie
            var token = HttpContext.Request.Cookies["NeoWebAppCookie"];

            if (token != null)
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return httpClient;
        }
    }
}
