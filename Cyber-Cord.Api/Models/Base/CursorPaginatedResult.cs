namespace Cyber_Cord.Api.Models.Base;

public class CursorPaginatedResult<T>
{
    public IReadOnlyList<T> Data { get; init; }
    public int TotalCount { get; init; }
    public int PageSize { get; init; }
    public CursorReturnModel Cursor {  get; init; }

    public CursorPaginatedResult(IReadOnlyList<T> data, int totalCount, int pageSize, DateTime cursorTime, int cursorId)
    {
        Data = data;
        TotalCount = totalCount;
        PageSize = pageSize;
        Cursor = new CursorReturnModel
        {
            Id = cursorId,
            Time = cursorTime,
        };
    }
}
