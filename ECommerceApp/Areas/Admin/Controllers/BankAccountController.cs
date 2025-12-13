using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BankAccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public BankAccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.BankAccounts.ToListAsync());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BankAccount bankAccount)
    {
        if (ModelState.IsValid)
        {
            _context.Add(bankAccount);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(bankAccount);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bankAccount = await _context.BankAccounts.FindAsync(id);
        if (bankAccount == null)
        {
            return NotFound();
        }
        return View(bankAccount);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BankAccount bankAccount)
    {
        if (id != bankAccount.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(bankAccount);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BankAccountExists(bankAccount.Id))
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
        return View(bankAccount);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bankAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(m => m.Id == id);
        if (bankAccount == null)
        {
            return NotFound();
        }

        return View(bankAccount);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var bankAccount = await _context.BankAccounts.FindAsync(id);
        if (bankAccount != null)
        {
            _context.BankAccounts.Remove(bankAccount);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool BankAccountExists(int id)
    {
        return _context.BankAccounts.Any(e => e.Id == id);
    }
}
