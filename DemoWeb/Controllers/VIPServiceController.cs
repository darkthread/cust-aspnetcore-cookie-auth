using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoWeb.Controllers
{
    [Authorize]
    public class VIPServiceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
