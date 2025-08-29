using Maliev.CountryService.Data.Models;
using Maliev.CountryService.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maliev.CountryService.Api.Services
{
    public interface ICountryService
    {
        Task<CountryDto> CreateCountryAsync(CreateCountryRequest request);
        Task<bool> DeleteCountryAsync(int id);
        Task<List<CountryDto>> GetAllCountriesAsync();
        Task<CountryDto?> GetCountryAsync(int id);
        Task<bool> UpdateCountryAsync(int id, UpdateCountryRequest request);
    }
}