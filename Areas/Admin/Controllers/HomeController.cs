using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Filters;
using WebBanSach.Models;

namespace WebBanSach.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Tổng quan
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCategories = await _context.ProductCategories.CountAsync();
            ViewBag.TotalPublishers = await _context.Publishers.CountAsync();
            ViewBag.TotalCustomers = await _context.AppUsers.CountAsync(u => u.UserType == UserType.Customer);
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalRevenue = await _context.OrderDetails
                                                 .Where(od => od.Order.Status == OrderStatus.Paid)
                                                 .SumAsync(od => (int?)od.Quantity * od.UnitPrice) ?? 0;


            // 10 ĐƠN HÀNG CHƯA XÁC NHẬN MỚI NHẤT
            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails) // thêm dòng này để đảm bảo không Null
                .Where(o => o.Status == OrderStatus.Pending)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new
                {
                    OrderId = o.ID,
                    o.OrderDate,
                    o.Status,
                    CustomerName = o.User.FullName,
                    TotalAmount = o.OrderDetails.Any() ? o.OrderDetails.Sum(od => od.Quantity * od.UnitPrice) : 0  
                })
                .ToListAsync();


            // Biểu đồ doanh thu theo tháng (giữ nguyên)
            var revenueData = await _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Order != null && od.Order.Status == OrderStatus.Paid)
                .GroupBy(od => od.Order.OrderDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .ToListAsync();


            ViewBag.MonthLabels = "[" + string.Join(",", revenueData.Select(d => $"\"Tháng {d.Month}\"")) + "]";
            ViewBag.MonthRevenue = "[" + string.Join(",", revenueData.Select(d => d.Total)) + "]";

            // Biểu đồ trạng thái đơn hàng
            var statusCounts = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();


            int pending = statusCounts.FirstOrDefault(s => s.Status == OrderStatus.Pending)?.Count ?? 0;
            int confirmed = statusCounts.FirstOrDefault(s => s.Status == OrderStatus.Confirmed)?.Count ?? 0;
            int paid = statusCounts.FirstOrDefault(s => s.Status == OrderStatus.Paid)?.Count ?? 0;
            int cancelled = statusCounts.FirstOrDefault(s => s.Status == OrderStatus.Cancelled)?.Count ?? 0;

            ViewBag.OrderStatusCounts = $"[{pending},{confirmed},{paid},{cancelled}]";

            return View();
        }
    }
}