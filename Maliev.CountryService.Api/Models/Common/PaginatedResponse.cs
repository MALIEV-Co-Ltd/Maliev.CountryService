namespace Maliev.CountryService.Api.Models.Common;

/// <summary>
/// Represents a paginated response for a collection of data.
/// </summary>
/// <typeparam name="T">The type of the data items in the page.</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// Gets or sets the data for the current page.
    /// </summary>
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    /// <summary>
    /// Gets or sets the current page number (1-indexed).
    /// </summary>
    public int Page { get; set; }
    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }
    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}