using System.ComponentModel.DataAnnotations;

namespace AspNetCoreApiStarter.Models;

public class PageQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 25;

    public string? Q { get; set; }
    public bool IncludeDeleted { get; set; }
}

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
