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

        public async Task<IActionResult> Index(string search, string status = "all", int? userId = null, int page = 1)
        {
            int pageSize = 10;

            var orders = _context.Orders
                                .Include(o => o.User)
                                .Include(o => o.OrderDetails)
                                .AsQueryable();

            // ========== 1. Lọc theo User ==========
            string customerName = "";

            if (userId.HasValue)
            {
                orders = orders.Where(o => o.UserId == userId.Value);

                var customer = await _context.AppUsers.FindAsync(userId.Value);

                customerName = customer?.FullName ?? $"Khách hàng ID {userId}";
            }

            // ========== 2. Search (chỉ dùng khi không có userId) ==========
            if (!string.IsNullOrWhiteSpace(search) && !userId.HasValue)
            {
                search = search.Trim().ToLower();

                orders = orders.Where(o =>
                    o.ID.ToString().Contains(search) ||
                    o.User.FullName.ToLower().Contains(search) ||
                    o.User.Email.ToLower().Contains(search) ||
                    (o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(search))
                );
            }

            // ========== 3. Filter status ==========
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                if (Enum.TryParse<OrderStatus>(status, out var st))
                {
                    orders = orders.Where(o => o.Status == st);
                }
            }

            // ========== 4. Order list ==========
            orders = orders.OrderByDescending(o => o.OrderDate);

            // ========== 5. Pagination ==========
            int totalOrders = await orders.CountAsync();
            int totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);

            var data = await orders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ========== 6. Gửi ra View ==========
            ViewBag.Search = search;
            ViewBag.Status = status;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.UserId = userId;
            ViewBag.CustomerName = customerName;
            ViewBag.TotalOrders = totalOrders;

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