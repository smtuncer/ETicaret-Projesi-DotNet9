using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.User.Controllers;
[Area("User")]
public class FaqController : Controller
{
    private readonly ApplicationDbContext _context;

    public FaqController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Route("sikca-sorulan-sorular")]
    public async Task<IActionResult> Index()
    {
        var faqs = await _context.Faqs.OrderBy(f => f.Order).ToListAsync();
        return View(faqs);
    }
}
