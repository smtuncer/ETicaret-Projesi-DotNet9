using AutoMapper;
using ECommerceApp.Models;
using ECommerceApp.Models.DTOs.Contact;

namespace ECommerceApp.Mappings;

public class ContactMappingProfile : Profile
{
    public ContactMappingProfile()
    {
        _ = CreateMap<SiteContent, ContactInfoDto>();
        _ = CreateMap<ContactFormDto, ContactMessage>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SentDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.IsReplied, opt => opt.MapFrom(_ => false));
    }
}

