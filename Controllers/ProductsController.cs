using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Models;

namespace WebBanSach.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product (danh sách sản phẩm, lọc theo categoryId, sắp xếp giá, phân trang)
        public async Task<IActionResult> Index(int? categoryId, string sortPrice = "default", string search="", int page = 1)
        {
            int pageSize = 12; // 12 sản phẩm/trang, giống hình bạn gửi

            // Lấy tất cả ProductCategory cho sidebar (để active khi chọn)
            var categories = await _context.ProductCategories.ToListAsync();

            // Query sản phẩm theo model Product (lọc theo CategoryId nếu có)
            var query = _context.Products
                .Include(p => p.Author)  // Include theo model
                .Include(p => p.Category)
                .Include(p => p.Publisher)  // Include Publisher nếu cần hiển thị
                .Where(p => p.Stock > 0);  // Chỉ sản phẩm có hàng

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // lọc theo search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Author.Name.Contains(search) ||
                    p.Category.Name.Contains(search));
            }

            // Sắp xếp theo giá (lọc giá theo yêu cầu)
            switch (sortPrice)
            {
                case "low-to-high":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "high-to-low":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                default:
                    query = query.OrderByDescending(p => p.ID); // Mới nhất mặc định
                    break;
            }

            // Phân trang
            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ViewBag cho view
            ViewBag.CategoryId = categoryId;
            ViewBag.SortPrice = sortPrice;
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Categories = categories;
            ViewBag.TotalProducts = totalProducts;

            return View(products);
        }
    

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Publisher)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy sách liên quan
            var related = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ID != id)
                .Take(6)
                .ToListAsync();

            ViewBag.RelatedBooks = related;

            return View(product);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ID == id);
        }
    }
}
