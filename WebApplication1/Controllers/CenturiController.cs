using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class CenturiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
