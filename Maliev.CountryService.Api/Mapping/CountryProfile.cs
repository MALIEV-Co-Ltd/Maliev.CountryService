using AutoMapper;
using Maliev.CountryService.Api.Models;
using Maliev.CountryService.Data.Entities;

namespace Maliev.CountryService.Api.Mapping;

public class CountryProfile : Profile
{
    public CountryProfile()
    {
        CreateMap<Country, CountryDto>()
            .ForMember(dest => dest.CountryCodes, opt => opt.MapFrom(src => src.CountryCodes));
        
        CreateMap<CountryCode, CountryCodeDto>();
    }
}