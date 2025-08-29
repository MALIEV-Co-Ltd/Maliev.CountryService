using System;
using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Data.Models
{
    /// <summary>
    /// Represents a country entity.
    /// </summary>
    public partial class Country
    {
        /// <summary>
        /// Gets or sets the unique identifier for the country.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the country.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the continent the country belongs to.
        /// </summary>
        public string Continent { get; set; }

        /// <summary>
        /// Gets or sets the country code.
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Gets or sets the ISO 2-letter code for the country.
        /// </summary>
        public string Iso2 { get; set; }

        /// <summary>
        /// Gets or sets the ISO 3-letter code for the country.
        /// </summary>
        public string Iso3 { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the country record.
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modification date of the country record.
        /// </summary>
        public DateTime? ModifiedDate { get; set; }
    }
}
