using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toast;

    public UserController(ApplicationDbContext context, IToastNotification toast)
    {
        _context = context;
        _toast = toast;
    }

    [Route("admin/users")]
    public async Task<IActionResult> Index(string search, string role, int page = 1)
    {
        const int pageSize = 10;

        var query = _context.Users.AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(u =>
                u.Name.ToLower().Contains(search) ||
                u.Surname.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
        }

        // Filter by role
        if (!string.IsNullOrWhiteSpace(role) && role != "All")
        {
            query = query.Where(u => u.Role == role);
        }

        // Get total count for pagination
        var totalUsers = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

        // Pagination
        var users = await query
            .OrderByDescending(u => u.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.TotalUsers = totalUsers;

        return View(users);
    }

    [Route("admin/users/details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _context.Users
            .Include(u => u.Addresses)
            .Include(u => u.Orders)
            .Include(u => u.PaymentNotifications)
            .Include(u => u.ProductComments)
                .ThenInclude(c => c.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            _toast.AddErrorToastMessage("Kullanıcı bulunamadı!");
            return RedirectToAction("Index");
        }

        var model = new Models.ViewModels.UserDetailsVM
        {
            User = user,
            Orders = user.Orders.OrderByDescending(o => o.OrderDate).ToList(),
            PaymentNotifications = user.PaymentNotifications.OrderByDescending(p => p.NotificationDate).ToList(),
            ProductComments = user.ProductComments.OrderByDescending(c => c.CreatedDate).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ECommerceApp.Models.User user, string Password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                _toast.AddErrorToastMessage("Şifre en az 6 karakter olmalıdır!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (existingUser != null)
            {
                _toast.AddErrorToastMessage("Bu e-posta adresi zaten kayıtlı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(Password);
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Kullanıcı eklendi!", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Kullanıcı eklenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ECommerceApp.Models.User user, string NewPassword)
    {
        try
        {
            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser == null)
            {
                _toast.AddErrorToastMessage("Kullanıcı bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            // Check if email is already taken by another user
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == user.Email && u.Id != user.Id);
            if (emailExists)
            {
                _toast.AddErrorToastMessage("Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            existingUser.Name = user.Name;
            existingUser.Surname = user.Surname;
            existingUser.Email = user.Email;
            existingUser.PhoneNumber = user.PhoneNumber;
            existingUser.Role = user.Role;

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                if (NewPassword.Length < 6)
                {
                    _toast.AddErrorToastMessage("Şifre en az 6 karakter olmalıdır!", new ToastrOptions { Title = "Hata" });
                    return RedirectToAction("Index");
                }
                existingUser.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            }

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Kullanıcı güncellendi!", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Kullanıcı güncellenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _toast.AddErrorToastMessage("Kullanıcı bulunamadı!");
                return RedirectToAction("Index");
            }

            // Prevent deleting yourself
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
            {
                _toast.AddErrorToastMessage("Kendi hesabınızı silemezsiniz!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            // Perform Soft Delete
            user.IsActive = false;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Kullanıcı başarıyla silindi (Pasife alındı).", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Kullanıcı silinirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> ToggleBlock(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _toast.AddErrorToastMessage("Kullanıcı bulunamadı!");
                return RedirectToAction("Index");
            }

            // Prevent blocking yourself
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
            {
                _toast.AddErrorToastMessage("Kendi hesabınızı engelleyemezsiniz!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            user.IsActive = !user.IsActive;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var message = user.IsActive ? "Kullanıcı engeli kaldırıldı!" : "Kullanıcı engellendi!";
            _toast.AddSuccessToastMessage(message, new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("İşlem sırasında bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }
}
