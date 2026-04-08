using Shared.Enums;

namespace Cyber_Cord.Api.Extensions;

public static class EnumerableExtensions
{
    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> enumerable, Ordering order, Func<TSource, TKey> func)
    {
        return order == Ordering.Asc
            ? enumerable.OrderBy(func)
            : enumerable.OrderByDescending(func);
    }
}
