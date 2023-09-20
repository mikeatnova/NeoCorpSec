using Microsoft.AspNetCore.Mvc;

namespace NeoCorpSec.Controllers
{
    public class TourController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
