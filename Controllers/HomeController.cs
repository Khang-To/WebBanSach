using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebBanSach.Models;

namespace WebBanSach.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {

            // 6 danh mục có lượt mua nhiều nhất (để hiển thị nổi bật)
            var categories = await _context.ProductCategories
                .Include(c => c.Products)
                .ThenInclude(p => p.OrderDetails)
                .OrderByDescending(c => c.Products.SelectMany(p => p.OrderDetails).Sum(od => od.Quantity))
                .Take(6)
                .ToListAsync();

            // 12 sách mới nhất
            var newBooks = await _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .OrderByDescending(p => p.ID)
                .Take(12)
                .ToListAsync();

            // 12 sách bán chạy nhất (tính theo tổng số lượng đã bán trong OrderDetail)
            var bestSellers = await _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.OrderDetails)
                .OrderByDescending(p => p.OrderDetails.Sum(od => od.Quantity))
                .Take(12)
                .ToListAsync();

            // 12 sách giảm giá mạnh nhất
            var discountBooks = await _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Where(p => p.DiscountPercent > 0 && p.Stock > 0)
                .OrderByDescending(p => p.DiscountPercent)
                .Take(12)
                .ToListAsync();

            // Dùng ViewBag truyền dữ liệu ra view
            ViewBag.Categories = categories;
            ViewBag.NewBooks = newBooks;
            ViewBag.BestSellers = bestSellers;
            ViewBag.DiscountBooks = discountBooks;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}