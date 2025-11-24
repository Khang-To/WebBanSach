using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Models;

public class CategoryMenuViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public CategoryMenuViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _context.ProductCategories
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }
}
