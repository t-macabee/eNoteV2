using System.Linq.Expressions;

namespace eNote.Application.Common.Search;

public static class QueryableFilterExtensions
{
    public static IQueryable<T> WhereContainsIf<T>(this IQueryable<T> query, string? value, Expression<Func<T, bool>> predicate) => string.IsNullOrWhiteSpace(value) ? query : query.Where(predicate);
    public static IQueryable<T> WhereEqualsIf<T, TValue>(this IQueryable<T> query, TValue? value, Expression<Func<T, bool>> predicate) where TValue : struct => value.HasValue ? query.Where(predicate) : query;
}