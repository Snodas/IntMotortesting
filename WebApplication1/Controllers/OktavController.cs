using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class OktavController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
