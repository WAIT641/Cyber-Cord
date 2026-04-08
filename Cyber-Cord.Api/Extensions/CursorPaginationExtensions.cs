using Cyber_Cord.Api.Models.Base;
using Cyber_Cord.Api.Types.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cyber_Cord.Api.Extensions;

public static class CursorPaginationExtensions
{
    public async static Task<CursorPaginatedResult<T>> ToCursorPaginatedAsync<T>(this IQueryable<T> query, CursorPaginationFilter filter) where T : class, ICursorPaginatable
    {
        var cursorTime = filter.Cursor!.Time ?? DateTime.MaxValue;
        var cursorId = filter.Cursor!.Id ?? int.MaxValue;

        var totalCount = await query.CountAsync();

        var time = cursorTime;
        var id = cursorId;
        var data = await query
            .Where(x => x.CreatedAt < time && x.Id < id)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(filter.PageSize)
            .ToListAsync();

        if (data.Any())
        {
            var last = data.Last();

            cursorTime = last.CreatedAt;
            cursorId = last.Id;
        }

        return new CursorPaginatedResult<T>(data, totalCount, filter.PageSize, cursorTime, cursorId);
    }
}
