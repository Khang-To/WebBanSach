using Microsoft.AspNetCore.Mvc;

namespace WebBanSach.Areas.Admin.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
