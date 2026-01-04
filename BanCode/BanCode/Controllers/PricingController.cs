using Microsoft.AspNetCore.Mvc;

namespace BanCode.Controllers
{
    public class PricingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}