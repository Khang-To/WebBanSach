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
    //[AdminAuthorize]
    public class AppUsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppUsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AppUsers
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            int pageSize = 10; // số lượng item trên 1 trang
            var query = _context.AppUsers.AsQueryable();

            // Tìm kiếm theo tên danh mục
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.FullName.Contains(search));
            }

            // Tính toán phân trang
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Lấy dữ liệu trang hiện tại
            var data = await query
                .OrderBy(x => x.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền dữ liệu sang View
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(data);
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
