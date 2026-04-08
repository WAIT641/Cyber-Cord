using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Cyber_Cord.Api.Attributes;
using Cyber_Cord.Api.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Cyber_Cord.Api.Extensions;

public static class PaginationExtensions
{
    private static readonly ConcurrentDictionary<(Type Type, string Property), object?> _selectorCache = new();

    public static async Task<PaginatedResult<T>> ToPaginatedAsync<T>(this IQueryable<T> query, PaginationFilter filter) where T : class
    {
        var totalCount = await query.CountAsync();
        var skip = (filter.PageNumber - 1) * filter.PageSize;

        if (!string.IsNullOrWhiteSpace(filter.OrderBy))
        {
            query = ApplyOrdering(query, filter.OrderBy);
        }

        var data = await query
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResult<T>(data, totalCount, filter.PageNumber, filter.PageSize);
    }

    private static IQueryable<T> ApplyOrdering<T>(IQueryable<T> query, string orderBy)
    {
        var sortColumns = orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var column in sortColumns)
        {
            var parts = column.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                continue;

            var propertyName = parts[0];
            var isDescending = parts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase);

            var keySelector = GetOrCreateSelector<T>(propertyName);
            if (keySelector == null)
                continue;

            if (orderedQuery == null)
            {
                orderedQuery = isDescending
                    ? query.OrderByDescending(keySelector)
                    : query.OrderBy(keySelector);
            }
            else
            {
                orderedQuery = isDescending
                    ? orderedQuery.ThenByDescending(keySelector)
                    : orderedQuery.ThenBy(keySelector);
            }
        }

        return orderedQuery ?? query;
    }

    private static Expression<Func<T, object>>? GetOrCreateSelector<T>(string propertyName)
    {
        var cacheKey = (typeof(T), propertyName.ToUpperInvariant());
        var cachedExpression = _selectorCache.GetOrAdd(cacheKey, _ => CreateSelector<T>(propertyName));
        return cachedExpression as Expression<Func<T, object>>;
    }

    private static Expression<Func<T, object>>? CreateSelector<T>(string propertyName)
    {
        var entityType = typeof(T);

        var propertyInfo = entityType.GetProperty(
            propertyName,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (propertyInfo == null)
            return null;

        if (!propertyInfo.IsDefined(typeof(SortableAttribute)))
            return null;

        var parameter = Expression.Parameter(entityType, "x");
        var propertyAccess = Expression.Property(parameter, propertyInfo);
        var convertToObject = Expression.Convert(propertyAccess, typeof(object));

        return Expression.Lambda<Func<T, object>>(convertToObject, parameter);
    }
}


