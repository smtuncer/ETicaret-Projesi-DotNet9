using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponController : Controller
{
    private readonly ApplicationDbContext _context;

    public CouponController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _context.Coupons.OrderByDescending(c => c.CreatedDate).AsQueryable();

        // Pagination
        int pageSize = 10;
        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var coupons = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;

        return View(coupons);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Coupon coupon)
    {
        if (ModelState.IsValid)
        {
            if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
            {
                ModelState.AddModelError("Code", "Bu kupon kodu zaten mevcut.");
                return View(coupon);
            }

            _context.Add(coupon);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(coupon);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon == null)
        {
            return NotFound();
        }
        return View(coupon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Coupon coupon)
    {
        if (id != coupon.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code && c.Id != id))
                {
                    ModelState.AddModelError("Code", "Bu kupon kodu zaten mevcut.");
                    return View(coupon);
                }

                _context.Update(coupon);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CouponExists(coupon.Id))
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
        return View(coupon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon != null)
        {
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool CouponExists(int id)
    {
        return _context.Coupons.Any(e => e.Id == id);
    }
}
