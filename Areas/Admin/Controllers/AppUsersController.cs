using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Models;

namespace WebBanSach.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AppUsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppUsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AppUsers
        public async Task<IActionResult> Index()
        {
            return View(await _context.AppUsers.ToListAsync());
        }

        // GET: Admin/AppUsers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(m => m.ID == id);
            if (appUser == null)
            {
                return NotFound();
            }

            return View(appUser);
        }

        // Hàm bật tắt trạng thái của khách hàng
        public IActionResult ToggleStatus(int id)
        {
            var user = _context.AppUsers.Find(id);
            if (user == null)
                return NotFound();

            user.IsActive = !user.IsActive; // Đảo trạng thái true/false
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }


        private bool AppUserExists(int id)
        {
            return _context.AppUsers.Any(e => e.ID == id);
        }
    }
}
