using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class OrganisationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
