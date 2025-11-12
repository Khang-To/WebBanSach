using Microsoft.EntityFrameworkCore;
using System;
using WebBanSach.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor(); // 

// Add services to the container.
builder.Services.AddControllersWithViews();

// Đăng ký DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Bật Session (nếu bạn có giỏ hàng)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1); // thời gian lưu session (ở server)
    options.Cookie.Name = ".WebBanSach.Session";  // tên cookie session
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//    if (!db.AppUsers.Any(u => u.UserType == UserType.Admin))
//    {
//        db.AppUsers.Add(new AppUser
//        {
//            UserName = "admin",
//            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), // mã hóa
//            FullName = "Quản trị viên",
//            Email = "admin@gmail.com",
//            UserType = UserType.Admin,
//            IsActive = true
//        });
//        db.SaveChanges();
//    }
//}

// Route cho Area (Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Route cho Area (Client)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
