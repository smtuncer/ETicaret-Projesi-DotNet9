using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceApp.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class FavoriteController : Controller
{
    private readonly ApplicationDbContext _context;

    public FavoriteController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Route("api/favorites/toggle")]
    public async Task<IActionResult> Toggle([FromBody] int productId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var existing = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

        bool isFav = false;
        if (existing != null)
        {
            _context.Favorites.Remove(existing);
            await _context.SaveChangesAsync();
            isFav = false;
        }
        else
        {
            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = productId,
                CreatedDate = DateTime.Now
            };
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
            isFav = true;
        }

        return Json(new { success = true, isFav = isFav });
    }

    [HttpGet]
    [Route("api/favorites/list")]
    public async Task<IActionResult> GetUserFavorites()
    {
        if (!User.Identity.IsAuthenticated)
            return Json(new List<int>());

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var productIds = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => f.ProductId)
            .ToListAsync();

        return Json(productIds);
    }
}
