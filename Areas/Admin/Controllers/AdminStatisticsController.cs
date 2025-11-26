using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebBanSach.Filters;

namespace WebBanSach.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class AdminStatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminStatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            // Mặc định: từ đầu tháng đến hết hôm nay
            fromDate ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            toDate ??= DateTime.Today.AddDays(1).AddSeconds(-1);

            // Chống ngày sai thứ tự
            if (fromDate.Value.Date > toDate.Value.Date)
            {
                var temp = fromDate;
                fromDate = toDate.Value.Date;
                toDate = temp.Value.Date.AddDays(1).AddSeconds(-1);
                TempData["Warning"] = "Ngày bắt đầu lớn hơn ngày kết thúc → đã tự động đảo ngược.";
            }

            var ordersInPeriod = _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate);

            var totalOrders = await ordersInPeriod.CountAsync();
            var totalPaidOrders = await ordersInPeriod.CountAsync(o => o.Status == OrderStatus.Paid);
            var totalCancelledOrders = await ordersInPeriod.CountAsync(o => o.Status == OrderStatus.Cancelled);

            // Tính tổng doanh thu
            var totalRevenue = await _context.OrderDetails
                .Where(od => od.Order.OrderDate >= fromDate
                          && od.Order.OrderDate <= toDate
                          && od.Order.Status == OrderStatus.Paid)
                .SumAsync(od => od.Quantity * od.UnitPrice);

            // ĐOẠN NÀY ĐỂ VẼ BIỂU ĐỒ
            var revenueByDay = await _context.OrderDetails
                .Where(od => od.Order.OrderDate >= fromDate &&
                             od.Order.OrderDate <= toDate &&
                             od.Order.Status == OrderStatus.Paid)
                .GroupBy(od => od.Order.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var labels = revenueByDay.Select(x => x.Date.ToString("dd/MM")).ToArray();
            var data = revenueByDay.Select(x => x.Total).ToArray();

            ViewBag.ChartLabels = $"[\"{string.Join("\", \"", labels)}\"]";
            ViewBag.ChartData = string.Join(", ", data);
            // === HẾT ===

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalPaidOrders = totalPaidOrders;
            ViewBag.TotalCancelledOrders = totalCancelledOrders;
            ViewBag.TotalRevenue = totalRevenue.ToString("N0");
            ViewBag.Period = $"{fromDate.Value:dd/MM/yyyy} → {toDate.Value:dd/MM/yyyy}";

            return View();
        }
    }
}