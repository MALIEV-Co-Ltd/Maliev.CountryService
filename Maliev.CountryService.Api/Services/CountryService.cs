using Maliev.CountryService.Data.Data;
using Maliev.CountryService.Data.Models;
using Maliev.CountryService.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maliev.CountryService.Api.Services
{
    public class CountryService : ICountryService
    {
        private readonly CountryContext _context;

        public CountryService(CountryContext context)
        {
            _context = context;
        }

        public async Task<CountryDto> CreateCountryAsync(CreateCountryRequest request)
        {
            var country = new Country
            {
                Continent = request.Continent,
                CountryCode = request.CountryCode,
                CreatedDate = DateTime.UtcNow,
                Iso2 = request.Iso2,
                Iso3 = request.Iso3,
                ModifiedDate = DateTime.UtcNow,
                Name = request.Name,
            };

            await _context.Country.AddAsync(country);
            await _context.SaveChangesAsync();

            return new CountryDto
            {
                Id = country.Id,
                Name = country.Name,
                Continent = country.Continent,
                CountryCode = country.CountryCode,
                Iso2 = country.Iso2,
                Iso3 = country.Iso3
            };
        }

        public async Task<bool> DeleteCountryAsync(int id)
        {
            var country = await _context.Country.FindAsync(id);
            if (country == null)
            {
                return false;
            }

            _context.Country.Remove(country);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CountryDto>> GetAllCountriesAsync()
        {
            var countries = await _context.Country.OrderBy(c => c.Name).ToListAsync();
            return countries.Select(c => new CountryDto
            {
                Id = c.Id,
                Name = c.Name,
                Continent = c.Continent,
                CountryCode = c.CountryCode,
                Iso2 = c.Iso2,
                Iso3 = c.Iso3
            }).ToList();
        }

        public async Task<CountryDto?> GetCountryAsync(int id)
        {
            var country = await _context.Country.FindAsync(id);
            if (country == null)
            {
                return null;
            }

            return new CountryDto
            {
                Id = country.Id,
                Name = country.Name,
                Continent = country.Continent,
                CountryCode = country.CountryCode,
                Iso2 = country.Iso2,
                Iso3 = country.Iso3
            };
        }

        public async Task<bool> UpdateCountryAsync(int id, UpdateCountryRequest request)
        {
            var country = await _context.Country.FindAsync(id);
            if (country == null)
            {
                return false;
            }

            country.Continent = request.Continent;
            country.CountryCode = request.CountryCode;
            country.Iso2 = request.Iso2;
            country.Iso3 = request.Iso3;
            country.ModifiedDate = DateTime.UtcNow;
            country.Name = request.Name;

            _context.Country.Update(country);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}