namespace Maliev.CountryService.Api.Models;

/// <summary>
/// Helper class for paginated results.
/// </summary>
/// <typeparam name="T">The type of the data being paginated.</typeparam>
internal class PagedResult<T>
{
    /// <summary>
    /// The data for the current page.
    /// </summary>
    public IEnumerable<T> Data { get; init; } = Enumerable.Empty<T>();

    /// <summary>
    /// The current page number (1-indexed).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// The total number of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Indicates if there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Indicates if there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Creates a new instance of <see cref="PagedResult{T}"/>.
    /// </summary>
    /// <param name="data">The data for the current page.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of items.</param>
    public PagedResult(IEnumerable<T> data, int page, int pageSize, int totalCount)
    {
        Data = data;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
