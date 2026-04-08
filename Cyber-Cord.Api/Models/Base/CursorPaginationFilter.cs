using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models.Base;

public class CursorPaginationFilter
{
    private const int _maxPageSize = 100;
    private const int _minPageSize = 1;
    private const int _defaultPageSize = 10;

    public CursorModel? Cursor { get; set; } = new();
    [Range(_minPageSize, _maxPageSize)]
    public int PageSize { get; set; } = _defaultPageSize;
}