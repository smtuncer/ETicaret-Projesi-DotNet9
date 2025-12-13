using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.User.Controllers
{
    [Area("User")]
    public class PolicyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PolicyController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Route("politikalar")]
        public async Task<IActionResult> Index()
        {
            var policies = await _context.Policies
                .Where(p => p.IsActive)
                .OrderBy(p => p.Order)
                .ToListAsync();
            return View(policies);
        }

        [Route("politika/{slug}")]
        public async Task<IActionResult> Detail(string slug)
        {
            var policy = await _context.Policies
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (policy == null)
            {
                return NotFound();
            }

            return View(policy);
        }
    }
}
