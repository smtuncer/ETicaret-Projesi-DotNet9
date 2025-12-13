using AutoMapper;
using ECommerceApp.Areas.User.ViewModels;
using ECommerceApp.Models.Data;
using ECommerceApp.Models.DTOs.Contact;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.ViewComponents
{
    public class FooterViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public FooterViewComponent(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Name)
                .ToListAsync();



            var contactContent = await _context.SiteContents
                .AsNoTracking()
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            var policies = await _context.Policies
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Order)
                .ToListAsync();

            var vm = new FooterViewModel
            {
                Categories = categories,
                Policies = policies,
                ContactInfo = contactContent != null
                    ? _mapper.Map<ContactInfoDto>(contactContent)
                    : new ContactInfoDto()
            };

            return View(vm);
        }
    }
}
