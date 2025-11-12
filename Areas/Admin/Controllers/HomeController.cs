using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Filters;
using WebBanSach.Models;

namespace WebBanSach.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[AdminAuthorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // Tổng quan
            ViewBag.TotalProducts = _db.Products.Count();
            ViewBag.TotalCategories = _db.ProductCategories.Count();
            ViewBag.TotalPublishers = _db.Publishers.Count();
            ViewBag.TotalCustomers = _db.AppUsers.Count(u => u.UserType == UserType.Customer);
            ViewBag.TotalOrders = _db.Orders.Count();
            ViewBag.TotalRevenue = _db.OrderDetails.Sum(od => od.Quantity * od.UnitPrice); // Đơn giá * số lượng


            // Recent Orders
            ViewBag.RecentOrders = _db.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new {
                    o.ID,
                    o.UserId,
                    CustomerName = o.User.FullName,
                    o.OrderDate,
                    o.Status
                }).ToList();

            // Recent Customers
            ViewBag.RecentCustomers = _db.AppUsers
                .Where(u => u.UserType == UserType.Customer)
                .OrderByDescending(u => u.ID)
                .Take(5)
                .Select(u => new {
                    u.FullName,
                    u.Email
                }).ToList();

            // Biểu đồ doanh thu theo tháng
            var revenueData = _db.OrderDetails
                .Include(od => od.Order)
                .GroupBy(od => od.Order.OrderDate.Month)
                .Select(g => new {
                    Month = g.Key,
                    Total = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderBy(g => g.Month)
                .ToList();

            ViewBag.MonthLabels = "[" + string.Join(",", revenueData.Select(d => $"\"Th{d.Month}\"")) + "]";
            ViewBag.MonthRevenue = "[" + string.Join(",", revenueData.Select(d => d.Total)) + "]";


            // Biểu đồ đơn hàng theo trạng thái
            var statusCounts = _db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            int pending = statusCounts.FirstOrDefault(s => s.Status == OrderStatus.Pending)?.Count ?? 0;
            int confirm = statusCounts.FirstOrDefault(s => s.Status == OrderStatus.Confirmed)?.Count ?? 0;
            int paid = statusCounts.FirstOrDefault(s => s.Status == OrderStatus.Paid)?.Count ?? 0;
            int cancelled = statusCounts.FirstOrDefault(s => s.Status == OrderStatus.Cancelled)?.Count ?? 0;

            ViewBag.OrderStatusCounts = $"[{pending},{confirm},{paid},{cancelled}]";

            return View();
        }
    }
}
