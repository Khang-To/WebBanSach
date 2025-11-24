using Microsoft.AspNetCore.Mvc;

namespace WebBanSach.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
