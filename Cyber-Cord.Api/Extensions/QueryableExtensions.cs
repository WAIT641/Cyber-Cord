using System.Linq.Expressions;
using Shared.Enums;

namespace Cyber_Cord.Api.Extensions;

public static class QueryableExtensions
{
    public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> queryable, Ordering order, Expression<Func<TSource, TKey>> func)
    {
        return order == Ordering.Asc
            ? queryable.OrderBy(func)
            : queryable.OrderByDescending(func);
    }
}
