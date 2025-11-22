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
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index(string search, string status = "all", int page = 1)
        {
            int pageSize = 10; // Số đơn mỗi trang

            var orders = _context.Orders
                            .Include(o => o.User)
                            .Include(o => o.OrderDetails)
                            .AsQueryable();

            // --- Tìm kiếm ---
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                orders = orders.Where(o =>
                    o.ID.ToString().Contains(search) ||
                    o.User.FullName.ToLower().Contains(search) ||
                    o.User.Email.ToLower().Contains(search) ||
                    (o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(search)));
            }

            // --- Lọc trạng thái ---
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                var stt = Enum.Parse<OrderStatus>(status);
                orders = orders.Where(o => o.Status == stt);
            }

            orders = orders.OrderByDescending(o => o.OrderDate);

            // --- Tổng số đơn ---
            int totalOrders = await orders.CountAsync();

            // --- Tổng số trang ---
            int totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);

            // --- Lấy dữ liệu của trang hiện tại ---
            var data = await orders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // --- Trả dữ liệu ra View ---
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.MaxPagesToShow = 5;

            return View(data);
        }


        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.ID == id.Value);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Cập nhật trạng thái (Xác nhận hoặc Hủy)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus Status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Không cho sửa nếu đã Paid hoặc đã Cancelled
            if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            {
                TempData["Error"] = "Đơn hàng đã hoàn tất hoặc đã hủy, không thể thay đổi!";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = Status;
            await _context.SaveChangesAsync();

            TempData["Success"] = Status == OrderStatus.Confirmed
                ? "Đã xác nhận đơn hàng!"
                : "Đã hủy đơn hàng!";

            return RedirectToAction(nameof(Details), new { id });
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.ID == id);
        }
    }
}
