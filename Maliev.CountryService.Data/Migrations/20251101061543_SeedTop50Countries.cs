using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CountryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedTop50Countries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed top 50 most populous countries with minimal data
            migrationBuilder.InsertData(
                table: "countries",
                columns: new[] { "iso2", "iso3", "name", "region" },
                values: new object[,]
                {
                    { "CN", "CHN", "China", "Asia" },
                    { "IN", "IND", "India", "Asia" },
                    { "US", "USA", "United States", "Americas" },
                    { "ID", "IDN", "Indonesia", "Asia" },
                    { "PK", "PAK", "Pakistan", "Asia" },
                    { "NG", "NGA", "Nigeria", "Africa" },
                    { "BR", "BRA", "Brazil", "Americas" },
                    { "BD", "BGD", "Bangladesh", "Asia" },
                    { "RU", "RUS", "Russia", "Europe" },
                    { "MX", "MEX", "Mexico", "Americas" },
                    { "JP", "JPN", "Japan", "Asia" },
                    { "ET", "ETH", "Ethiopia", "Africa" },
                    { "PH", "PHL", "Philippines", "Asia" },
                    { "EG", "EGY", "Egypt", "Africa" },
                    { "VN", "VNM", "Vietnam", "Asia" },
                    { "CD", "COD", "DR Congo", "Africa" },
                    { "TR", "TUR", "Turkey", "Asia" },
                    { "IR", "IRN", "Iran", "Asia" },
                    { "DE", "DEU", "Germany", "Europe" },
                    { "TH", "THA", "Thailand", "Asia" },
                    { "GB", "GBR", "United Kingdom", "Europe" },
                    { "FR", "FRA", "France", "Europe" },
                    { "IT", "ITA", "Italy", "Europe" },
                    { "ZA", "ZAF", "South Africa", "Africa" },
                    { "TZ", "TZA", "Tanzania", "Africa" },
                    { "MM", "MMR", "Myanmar", "Asia" },
                    { "KE", "KEN", "Kenya", "Africa" },
                    { "KR", "KOR", "South Korea", "Asia" },
                    { "CO", "COL", "Colombia", "Americas" },
                    { "ES", "ESP", "Spain", "Europe" },
                    { "UG", "UGA", "Uganda", "Africa" },
                    { "AR", "ARG", "Argentina", "Americas" },
                    { "DZ", "DZA", "Algeria", "Africa" },
                    { "SD", "SDN", "Sudan", "Africa" },
                    { "UA", "UKR", "Ukraine", "Europe" },
                    { "CA", "CAN", "Canada", "Americas" },
                    { "PL", "POL", "Poland", "Europe" },
                    { "MA", "MAR", "Morocco", "Africa" },
                    { "IQ", "IRQ", "Iraq", "Asia" },
                    { "AF", "AFG", "Afghanistan", "Asia" },
                    { "PE", "PER", "Peru", "Americas" },
                    { "SA", "SAU", "Saudi Arabia", "Asia" },
                    { "UZ", "UZB", "Uzbekistan", "Asia" },
                    { "MY", "MYS", "Malaysia", "Asia" },
                    { "VE", "VEN", "Venezuela", "Americas" },
                    { "AO", "AGO", "Angola", "Africa" },
                    { "GH", "GHA", "Ghana", "Africa" },
                    { "NP", "NPL", "Nepal", "Asia" },
                    { "YE", "YEM", "Yemen", "Asia" },
                    { "MG", "MDG", "Madagascar", "Africa" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seed data (delete by ISO2 code)
            migrationBuilder.DeleteData(
                table: "countries",
                keyColumn: "iso2",
                keyValues: new object[]
                {
                    "CN", "IN", "US", "ID", "PK", "NG", "BR", "BD", "RU", "MX",
                    "JP", "ET", "PH", "EG", "VN", "CD", "TR", "IR", "DE", "TH",
                    "GB", "FR", "IT", "ZA", "TZ", "MM", "KE", "KR", "CO", "ES",
                    "UG", "AR", "DZ", "SD", "UA", "CA", "PL", "MA", "IQ", "AF",
                    "PE", "SA", "UZ", "MY", "VE", "AO", "GH", "NP", "YE", "MG"
                });
        }
    }
}
