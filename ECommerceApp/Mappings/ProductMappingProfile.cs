using AutoMapper;
using ECommerceApp.Models;
using ECommerceApp.Models.DTOs.Product;

namespace ECommerceApp.Mappings;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        _ = CreateMap<Product, ProductDetailDto>()
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.Name : null))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));

        _ = CreateMap<ProductAttribute, ProductAttributeDto>();

        _ = CreateMap<Product, ProductSummaryDto>()
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.Name : null))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.PrimaryImageUrl,
                opt => opt.MapFrom(src => src.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()));
    }
}

