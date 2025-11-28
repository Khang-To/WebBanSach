using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using WebBanSach.Models;

namespace WebBanSach.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Đọc giỏ từ session HOẶC từ database nếu đã login
        private List<CartViewModel> GetCartItems()
        {
            //  Nếu đã login lấy giỏ từ database
            var userId = HttpContext.Session.GetInt32("Customer_UserId");
            if (userId.HasValue)
            {
                var dbItems = _context.CartItems
                    .Where(x => x.UserId == userId.Value)
                    .Include(x => x.Product)  // Quan trọng: phải Include Product
                    .ToList();

                var cartFromDb = dbItems.Select(x => new CartViewModel
                {
                    Product = x.Product,
                    Quantity = x.Quantity
                }).ToList();

                if (cartFromDb.Any())
                    return cartFromDb;
            }

            // Lưu giỏ vào session
            var session = HttpContext.Session;
            string? jsoncart = session.GetString("shopcart");
            if (jsoncart != null)
            {
                var cartItems = JsonConvert.DeserializeObject<List<CartViewModel>>(jsoncart);
                return cartItems ?? new List<CartViewModel>();
            }
            return new List<CartViewModel>();
        }

        // Hàm lưu giỏ vào session
        private void SaveCartSession(List<CartViewModel> list)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            var session = HttpContext.Session;
            string jsoncart = JsonConvert.SerializeObject(list, settings); // thêm settings
            session.SetString("shopcart", jsoncart);
        }

        // Xóa session giỏ hàng: xóa hết session luôn
        private void ClearCart()
        {
            HttpContext.Session.Remove("shopcart");
        }

        // GET: Xem giỏ hàng (giống ViewCart)
        public IActionResult ViewCart(int page = 1, int pageSize = 5)
        {
            var cart = GetCartItems();
            int totalItems = cart.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Slice cart cho page hiện tại
            var pagedCart = cart.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Truyền thêm info cho view (totalPages, currentPage, totalItems)
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            return View(pagedCart);
        }

        // POST: Thêm vào giỏ (thêm quantity param, check stock)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ID == id);
            if (product == null)
                return NotFound("Sản phẩm không tồn tại");

            if (quantity > product.Stock)
            {
                TempData["Error"] = $"Chỉ còn {product.Stock} sản phẩm trong kho";
                return RedirectToAction(nameof(ViewCart));
            }

            var userId = HttpContext.Session.GetInt32("Customer_UserId");

            if (userId.HasValue)
            {
                // Lấy item từ DB
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(x => x.UserId == userId.Value && x.ProductId == id);

                if (cartItem != null)
                {
                    var newQty = cartItem.Quantity + quantity;
                    if (newQty > product.Stock)
                    {
                        TempData["Error"] = $"Tổng số lượng vượt quá tồn kho ({product.Stock})";
                        return RedirectToAction(nameof(ViewCart));
                    }
                    cartItem.Quantity = newQty;
                }
                else
                {
                    // Thêm mới vào DB
                    var newItem = new CartItem
                    {
                        UserId = userId.Value,
                        ProductId = id,
                        Quantity = quantity
                    };
                    _context.CartItems.Add(newItem);
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                // Chưa login, lưu session
                var cart = GetCartItems();
                var item = cart.FirstOrDefault(p => p.Product.ID == id);
                if (item != null)
                {
                    var newQty = item.Quantity + quantity;
                    if (newQty > product.Stock)
                    {
                        TempData["Error"] = $"Tổng số lượng vượt quá tồn kho ({product.Stock})";
                        return RedirectToAction(nameof(ViewCart));
                    }
                    item.Quantity = newQty;
                }
                else
                {
                    cart.Add(new CartViewModel { Product = product, Quantity = quantity });
                }
                SaveCartSession(cart);
            }

            return RedirectToAction(nameof(ViewCart));
        }


        // POST: Cập nhật số lượng giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateItem(int id, int quantity)
        {
            var userId = HttpContext.Session.GetInt32("Customer_UserId");
            if (userId.HasValue)
            {
                var cartItem = _context.CartItems.FirstOrDefault(x => x.UserId == userId.Value && x.ProductId == id);
                if (cartItem != null)
                {
                    if (quantity <= 0)
                    {
                        _context.CartItems.Remove(cartItem);
                    }
                    else
                    {
                        var product = _context.Products.FirstOrDefault(p => p.ID == id);
                        if (product != null && quantity > product.Stock)
                        {
                            TempData["Error"] = $"Chỉ còn {product.Stock} sản phẩm trong kho";
                            return RedirectToAction(nameof(ViewCart));
                        }
                        cartItem.Quantity = quantity;
                    }
                    _context.SaveChanges();
                }
            }
            else
            {
                // Nếu chưa login, update session
                var cart = GetCartItems();
                var item = cart.FirstOrDefault(p => p.Product.ID == id);
                if (item != null)
                {
                    if (quantity <= 0) cart.Remove(item);
                    else item.Quantity = quantity;
                }
                SaveCartSession(cart);
            }

            return RedirectToAction(nameof(ViewCart));
        }


        // POST: Xóa item ở giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveItem(int id)
        {
            var userId = HttpContext.Session.GetInt32("Customer_UserId");
            if (userId.HasValue)
            {
                var cartItem = _context.CartItems.FirstOrDefault(x => x.UserId == userId.Value && x.ProductId == id);
                if (cartItem != null)
                {
                    _context.CartItems.Remove(cartItem);
                    _context.SaveChanges();
                }
            }
            else
            {
                var cart = GetCartItems();
                var item = cart.FirstOrDefault(p => p.Product.ID == id);
                if (item != null)
                {
                    cart.Remove(item);
                    SaveCartSession(cart);
                }
            }

            return RedirectToAction(nameof(ViewCart));
        }


        //// GET: Checkout (chuyển cart sang view)
        //public IActionResult CheckOut()
        //{
        //    var cart = GetCartItems();
        //    if (!cart.Any())
        //    {
        //        TempData["Error"] = "Giỏ hàng rỗng!";
        //        return RedirectToAction(nameof(ViewCart));
        //    }
        //    return View(cart);
        //}

        //// POST: Tạo đơn hàng (giống CreateBill, tạo AppUser tạm nếu guest)
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[ActionName("CreateBill")]
        //public async Task<IActionResult> CreateBill(string email, string hoten, string dienthoai, string? diachi)
        //{
        //    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(hoten))
        //    {
        //        TempData["Error"] = "Vui lòng nhập email và họ tên";
        //        return RedirectToAction(nameof(CheckOut));
        //    }

        //    // Tạo AppUser tạm nếu guest (như Khachhang)
        //    var user = new AppUser
        //    {
        //        UserName = hoten,  // Tạm dùng họ tên làm username
        //        PasswordHash = "guest",  // Không dùng thật
        //        FullName = hoten,
        //        Email = email,
        //        PhoneNumber = dienthoai,
        //        Address = diachi,
        //        UserType = UserType.Customer,
        //        IsActive = true
        //    };
        //    _context.AppUsers.Add(user);
        //    await _context.SaveChangesAsync();

        //    // Tạo Order
        //    var order = new Order
        //    {
        //        UserId = user.ID,
        //        OrderDate = DateTime.Now,
        //        Status = OrderStatus.Pending,
        //        Note = diachi  // Tạm dùng địa chỉ làm note
        //    };
        //    _context.Orders.Add(order);
        //    await _context.SaveChangesAsync();

        //    // Thêm OrderDetail từ cart
        //    var cart = GetCartItems();

        //    decimal thanhtien = 0;
        //    decimal tongtien = 0;
        //    foreach (var i in cart)
        //    {
        //        var detail = new OrderDetail
        //        {
        //            OrderId = order.ID,
        //            ProductId = i.Product.ID,
        //            Quantity = i.Quantity,
        //            UnitPrice = i.Product.Price  // Giá gốc, hoặc FinalPrice nếu có discount
        //        };

        //        thanhtien = detail.UnitPrice * detail.Quantity;
        //        thanhtien = i.Subtotal;  
        //        tongtien += thanhtien;
        //        _context.OrderDetails.Add(detail);
        //    }
        //    await _context.SaveChangesAsync();

        //    // Cập nhật tổng tiền Order (nếu model Order có TongTien)
        //    // order.TongTien = tongtien;  // Uncomment nếu có field
        //    // _context.Update(order);
        //    // await _context.SaveChangesAsync();

        //    ClearCart();
        //    return View(order);  // View xác nhận đơn hàng
        //}

        //// Merge session vào CartItem DB khi login (thêm mới)
        //public async Task MergeCartSessionToDatabase(int userId)
        //{
        //    var sessionCart = GetCartItems();
        //    if (!sessionCart.Any()) return;

        //    var dbCart = await _context.CartItems.Where(x => x.UserId == userId).ToListAsync();

        //    foreach (var item in sessionCart)
        //    {
        //        var existing = dbCart.FirstOrDefault(x => x.ProductId == item.Product.ID);
        //        if (existing == null)
        //        {
        //            _context.CartItems.Add(new CartItem { UserId = userId, ProductId = item.Product.ID, Quantity = item.Quantity });
        //        }
        //        else
        //        {
        //            existing.Quantity += item.Quantity;
        //        }
        //    }
        //    await _context.SaveChangesAsync();
        //    ClearCart();
        //}

    }
}