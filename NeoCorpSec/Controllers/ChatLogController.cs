using Microsoft.AspNetCore.Mvc;

namespace NeoCorpSec.Controllers
{
    public class ChatLogController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
