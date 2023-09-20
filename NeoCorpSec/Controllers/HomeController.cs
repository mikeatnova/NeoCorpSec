using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoCorpSec.Models;
using System.Diagnostics;

namespace NeoCorpSec.Controllers
{
    [Authorize(Roles = "Neo, SecurityChief, SecurityManager, SecuritySupervisor, SecurityOfficer")]
    public class HomeController : CoreController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult TourPage()
        {
            return View();
        }

        public IActionResult CameraList()
        {
            return View();
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