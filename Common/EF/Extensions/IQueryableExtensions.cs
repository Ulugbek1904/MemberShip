using Common.Exceptions;
using Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Common.EF.Extensions;

public static class IQueryableExtensions
{
    public static async Task<T?> GetByIdAsync<T, TId>(this IQueryable<T> queryable, TId id) where T : ModelBase<TId> where TId : struct
    {
        return await queryable.FirstOrDefaultAsync((T x) => x.Id.Equals(id));
    }

    public static Task<bool> ExistsAsync<T, TId>(this IQueryable<T> queryable, TId id) where T : ModelBase<TId> where TId : struct
    {
        return queryable.AnyAsync((T x) => x.Id.Equals(id));
    }

    public static async Task ExistsOrThrowsNotFoundException<T, TId>(this IQueryable<T> queryable, TId id) where T : ModelBase<TId> where TId : struct
    {
        if (!(await queryable.ExistsAsync(id)))
        {
            throw new NotFoundException($"{typeof(T).Name} not found by {id}");
        }
    }

    public static async Task<T> GetByIdOrThrowsNotFoundException<T, TId>(this IQueryable<T> queryable, TId id) where T : ModelBase<TId> where TId : struct
    {
        T obj = await queryable.GetByIdAsync(id);
        NotFoundException.ThrowIfNull(obj, $"{typeof(T).Name} not found by {id}");
        return obj;
    }
}
