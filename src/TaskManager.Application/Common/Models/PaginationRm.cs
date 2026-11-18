namespace TaskManager.Application.Common.Models;

public class PaginationRm
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    // Keeps (Page - 1) * PageSize inside int range when it becomes the SQL OFFSET.
    public const int MaxPage = int.MaxValue / MaxPageSize;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
}
