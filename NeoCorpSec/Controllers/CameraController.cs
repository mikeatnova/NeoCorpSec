using Microsoft.AspNetCore.Mvc;

namespace NeoCorpSec.Controllers
{
    public class CameraController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
