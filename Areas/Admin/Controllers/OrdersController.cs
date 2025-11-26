using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebBanSach.Filters;
using WebBanSach.Models;

namespace WebBanSach.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
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
                        .ThenInclude(p => p.Publisher)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(o => o.ID == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // POST: Admin cập nhật trạng thái (Xác nhận hoặc Hủy)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.ID == id);

            if (order == null) return NotFound();

            // Chỉ khóa khi đã Paid hoặc đã Cancelled (giữ nguyên logic bạn)
            if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            {
                TempData["Error"] = "Đơn hàng đã hoàn tất hoặc đã hủy, không thể thay đổi trạng thái!";
                return RedirectToAction("Details", new { id });
            }

            // TRƯỜNG HỢP HỦY ĐƠN (Pending hoặc Confirmed đều được)
            if (newStatus == OrderStatus.Cancelled)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Cộng lại tồn kho
                    foreach (var item in order.OrderDetails)
                    {
                        item.Product.Stock += item.Quantity;
                    }

                    order.Status = OrderStatus.Cancelled;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = $"Đã hủy đơn hàng #{order.ID} và hoàn lại tồn kho thành công!";
                }
                catch
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Lỗi khi hủy đơn hàng!";
                }
            }
            else if (newStatus == OrderStatus.Confirmed)
            {
                // Chỉ cho xác nhận khi đang Pending
                if (order.Status != OrderStatus.Pending)
                {
                    TempData["Error"] = "Chỉ xác nhận được đơn hàng đang Chờ xác nhận!";
                    return RedirectToAction("Details", new { id });
                }

                order.Status = OrderStatus.Confirmed;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xác nhận đơn hàng #{order.ID}!";
            }
            else
            {
                // Paid hoặc trạng thái khác
                order.Status = newStatus;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật trạng thái thành công!";
            }

            return RedirectToAction("Details", new { id });
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.ID == id);
        }
    }
}