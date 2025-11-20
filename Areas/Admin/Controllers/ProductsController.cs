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
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index(string? search, int? authorId, int? productCategoryId, int? publisherId, int page = 1)
        {
            int pageSize = 5;
            var query = _context.Products
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Publisher)
                .AsQueryable();

            // LỌC THEO TÊN SÁCH - CHỈ KHI NGƯỜI DÙNG THỰC SỰ GÕ GÌ ĐÓ
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.Name.Contains(search.Trim()));
            }

            // LỌC THEO TÁC GIẢ - LUÔN CHẠY, DÙ CÓ SEARCH HAY KHÔNG
            if (authorId.HasValue && authorId.Value > 0)
            {
                query = query.Where(x => x.AuthorId == authorId.Value);
            }

            // LỌC THEO THỂ LOẠI
            if (productCategoryId.HasValue && productCategoryId.Value > 0)
            {
                query = query.Where(x => x.CategoryId == productCategoryId.Value);
            }

            // LỌC THEO NHÀ XUẤT BẢN
            if (publisherId.HasValue && publisherId.Value > 0)
            {
                query = query.Where(x => x.PublisherId == publisherId.Value);
            }

            // Tính tổng và phân trang từ query ĐÃ LỌC
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var data = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy tên đã chọn để hiển thị thông báo "không tìm thấy"
            var selectedAuthorName = authorId.HasValue && authorId.Value > 0
                ? await _context.Authors.Where(a => a.ID == authorId.Value).Select(a => a.Name).FirstOrDefaultAsync()
                : null;

            var selectedCategoryName = productCategoryId.HasValue && productCategoryId.Value > 0
                ? await _context.ProductCategories.Where(c => c.ID == productCategoryId.Value).Select(c => c.Name).FirstOrDefaultAsync()
                : null;

            var selectedPublisherName = publisherId.HasValue && publisherId.Value > 0
                ? await _context.Publishers.Where(p => p.ID == publisherId.Value).Select(p => p.Name).FirstOrDefaultAsync()
                : null;

            // Truyền sang View
            ViewBag.Search = search?.Trim();
            ViewBag.AuthorId = authorId;
            ViewBag.ProductCategoryId = productCategoryId;
            ViewBag.PublisherId = publisherId;
            ViewBag.AuthorName = selectedAuthorName;
            ViewBag.ProductCategoryName = selectedCategoryName;
            ViewBag.PublisherName = selectedPublisherName;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            // Load dropdown
            ViewBag.Authors = await _context.Authors.OrderBy(a => a.Name).ToListAsync();
            ViewBag.ProductCategories = await _context.ProductCategories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Publishers = await _context.Publishers.OrderBy(p => p.Name).ToListAsync();

            return View(data);
        }

        // GET: Admin/Products/Details/5
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

            return View(product);
        }

        // hàm upload file hình
        private string? UploadImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            // Kiểm tra định dạng file
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" &&
                extension != ".png" && extension != ".gif" && extension != ".webp")
            {
                return null;
            }

            // Tạo tên file duy nhất
            var fileName = Guid.NewGuid().ToString() + extension;

            // Đường dẫn vật lý 
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            Directory.CreateDirectory(uploadFolder); // tạo thư mục nếu chưa có

            var filePath = Path.Combine(uploadFolder, fileName);

            // Lưu file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // Trả về đường dẫn để hiển thị trên web
            return fileName;
        }

        // GET: Admin/Products/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_context.Authors, "ID", "Name");
            ViewData["CategoryId"] = new SelectList(_context.ProductCategories, "ID", "Name");
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "ID", "Name");
            return View();
        }

        // POST: Admin/Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,Description,Price,DiscountPercent,Stock,ImageUrl,PublishYear,TotalPage,CategoryId,AuthorId,PublisherId")] Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                // gọi hàm upload
                var uploadedUrl = UploadImage(imageFile);

                // Nếu upload thành công thì gán URL, không thì dùng no-image
                product.ImageUrl = uploadedUrl ?? "no-image.webp";

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Authors, "ID", "Name", product.AuthorId);
            ViewData["CategoryId"] = new SelectList(_context.ProductCategories, "ID", "Name", product.CategoryId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "ID", "Name", product.PublisherId);
            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["AuthorId"] = new SelectList(_context.Authors, "ID", "Name", product.AuthorId);
            ViewData["CategoryId"] = new SelectList(_context.ProductCategories, "ID", "Name", product.CategoryId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "ID", "Name", product.PublisherId);
            return View(product);
        }

        // POST: Admin/Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Description,Price,DiscountPercent,Stock,ImageUrl,PublishYear,TotalPage,CategoryId,AuthorId,PublisherId")] Product product, IFormFile? imageFile)
        {
            if (id != product.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Có ảnh mới upload và cập nhật đường dẫn
                        var newImageUrl = UploadImage(imageFile);
                        if (newImageUrl != null)
                        {
                            // Xóa ảnh cũ để tiết kiệm dung lượng
                            if (!string.IsNullOrEmpty(product.ImageUrl) &&
                                product.ImageUrl != "no-image.webp" &&
                                System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", product.ImageUrl)))
                            {
                                System.IO.File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", product.ImageUrl));
                            }

                            product.ImageUrl = newImageUrl; 
                        }
                    }
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Authors, "ID", "Name", product.AuthorId);
            ViewData["CategoryId"] = new SelectList(_context.ProductCategories, "ID", "Name", product.CategoryId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "ID", "Name", product.PublisherId);
            return View(product);
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // xóa luôn file hình trong folder
                if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl != "no-image.webp")
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", product.ImageUrl);
                    if (System.IO.File.Exists(imagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(imagePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Không xóa được ảnh: {ex.Message}");
                        }
                    }
                }
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ID == id);
        }
    }
}
