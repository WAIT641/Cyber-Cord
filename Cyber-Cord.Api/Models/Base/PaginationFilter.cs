namespace Cyber_Cord.Api.Models.Base;

public class PaginationFilter
{
    private const int _maxPageSize = 100;
    private const int _minSize = 1;
    private const int _defaultPageSize = 10;

    public int PageNumber { get; set; } = _minSize;
    public int PageSize { get; set; } = _defaultPageSize;
    public string? OrderBy { get; set; }

    public PaginationFilter()
    {
        ValidateAndNormalize();
    }

    private void ValidateAndNormalize()
    {
        if (PageNumber < _minSize)
            PageNumber = _minSize;
        if (PageSize < _minSize)
            PageSize = _defaultPageSize;
        if (PageSize > _maxPageSize)
            PageSize = _maxPageSize;
    }
}

