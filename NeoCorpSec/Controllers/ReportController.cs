using Microsoft.AspNetCore.Mvc;

namespace NeoCorpSec.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
