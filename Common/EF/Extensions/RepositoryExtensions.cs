//using System.Linq.Expressions;
//using System.Reflection;
//using Common.Common.Models;
//using Common.EF.Generics;
//using Microsoft.EntityFrameworkCore;

//namespace Common.EF.Extensions;

//public static class RepositoryExtensions
//{
//    private sealed class LeftJoinInternal<TLeft, TRight>
//    {
//        public TLeft L;

//        public IEnumerable<TRight> R;
//    }

//    private static readonly List<string> ExpressionsList = new List<string>
//    {
//        "==", "!=", "<>", ">>", "<<", "<=", ">=", "$$", "$%", "%$",
//        "@@"
//    };

//    public static async Task<PagedCollection<T>> ToDataQueryAsync<T>(this IQueryable<T> queryable, DataQueryRequest query)
//    {
//        IQueryable<T> filtered = queryable.FilterByExpressions(query.FilteringExpression);
//        return new PagedCollection<T>(total: await filtered.CountAsync(), items: await filtered.Sort(query).Page(query).ToListAsync());
//    }

//    public static async Task<(IEnumerable<T>, int total, DataQueryRequest query)> GetByDataQueryAsync<T>(this IQueryable<T> queryable, DataQueryRequest query)
//    {
//        IQueryable<T> filtered = queryable.FilterByExpressions(query.FilteringExpression);
//        return new ValueTuple<IEnumerable<T>, int, DataQueryRequest>(item2: await filtered.CountAsync(), item1: await filtered.Sort(query).Page(query).ToListAsync(), item3: query);
//    }

//    public static IQueryable<T> FilterByExpressions<T>(this IQueryable<T> queryable, List<string>? expressions)
//    {
//        if (expressions?.DefaultIfEmpty() == null)
//        {
//            return queryable;
//        }

//        foreach (string expression2 in expressions)
//        {
//            string[] array = expression2.Split(ExpressionsList.ToArray(), StringSplitOptions.RemoveEmptyEntries);
//            string text = expression2.Substring(array[0].Length, 2);
//            if (array.Length < 2 || !ExpressionsList.Contains(text))
//            {
//                continue;
//            }

//            var anon = new
//            {
//                PropertyPath = array[0],
//                Value = array[1]
//            };
//            string[] array2 = anon.PropertyPath.Split('.');
//            Expression expression = Expression.Parameter(typeof(T), "x");
//            ParameterExpression parameterExpression = (ParameterExpression)expression;
//            string[] array3 = array2;
//            foreach (string text2 in array3)
//            {
//                PropertyInfo property = expression.Type.GetProperty(text2, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
//                if ((object)property == null)
//                {
//                    throw new Exception($"Property '{text2}' not found in type '{expression.Type.Name}'");
//                }

//                expression = Expression.MakeMemberAccess(expression, property);
//            }

//            MethodInfo method = typeof(NpgsqlDbFunctionsExtensions).GetMethod("ILike", new Type[3]
//            {
//                typeof(DbFunctions),
//                typeof(string),
//                typeof(string)
//            });
//            ConstantExpression constantExpression = Expression.Constant(Microsoft.EntityFrameworkCore.EF.Functions);
//            object obj = anon.Value;
//            Type type = expression.Type;
//            if (type == typeof(TimeOnly))
//            {
//                if (!TimeOnly.TryParse(anon.Value, out var result))
//                {
//                    throw new ArgumentException("Invalid TimeOnly value: " + anon.Value);
//                }

//                obj = result.ToTimeSpan();
//                expression = Expression.Call(expression, typeof(TimeOnly).GetMethod("ToTimeSpan"));
//            }
//            else if (type.IsValueType)
//            {
//                if (string.IsNullOrEmpty(anon.Value))
//                {
//                    obj = Activator.CreateInstance(type);
//                }
//                else
//                {
//                    Type underlyingType = Nullable.GetUnderlyingType(type);
//                    obj = (((object)underlyingType == null) ? Convert.ChangeType(anon.Value, type) : Convert.ChangeType(anon.Value, underlyingType));
//                }
//            }

//            bool flag = type == typeof(MultiLanguageField);
//            ConstantExpression right = Expression.Constant(obj, ((object)Nullable.GetUnderlyingType(type) != null) ? typeof(Nullable<>).MakeGenericType(obj?.GetType()) : obj?.GetType());
//            Expression<Func<T, bool>> predicate = Expression.Lambda<Func<T, bool>>(text switch
//            {
//                "!=" => Expression.NotEqual(expression, right),
//                "<>" => Expression.NotEqual(expression, right),
//                ">=" => Expression.GreaterThanOrEqual(expression, right),
//                "<=" => Expression.LessThanOrEqual(expression, right),
//                "<<" => Expression.LessThan(expression, right),
//                ">>" => Expression.GreaterThan(expression, right),
//                "==" => Expression.Equal(expression, right),
//                "$$" => flag ? MakeMultiLanguageFieldContains(expression, method, constantExpression, anon.Value, text) : Expression.Call(method, constantExpression, expression, Expression.Constant("%" + anon.Value + "%")),
//                "$%" => Expression.Call(method, constantExpression, expression, Expression.Constant(anon.Value + "%")),
//                "%$" => Expression.Call(method, constantExpression, expression, Expression.Constant("%" + anon.Value)),
//                _ => Expression.Equal(expression, right),
//            }, new ParameterExpression[1] { parameterExpression });
//            queryable = queryable.Where(predicate);
//        }

//        return queryable;
//    }

//    private static Expression MakeMultiLanguageFieldContains(Expression left, MethodInfo? ilikeMethod, Expression efFunctions, string value, string expressionStrType)
//    {
//        MemberExpression arg = Expression.MakeMemberAccess(left, typeof(MultiLanguageField).GetProperty("Ru"));
//        MemberExpression arg2 = Expression.MakeMemberAccess(left, typeof(MultiLanguageField).GetProperty("Uz"));
//        MemberExpression arg3 = Expression.MakeMemberAccess(left, typeof(MultiLanguageField).GetProperty("Eng"));
//        MemberExpression arg4 = Expression.MakeMemberAccess(left, typeof(MultiLanguageField).GetProperty("Cyrl"));
//        Expression arg5 = Expression.Constant(expressionStrType switch
//        {
//            "$$" => "%" + value + "%",
//            "$%" => value + "%",
//            "%$" => "%" + value,
//            _ => "%" + value + "%",
//        });
//        MethodCallExpression left2 = Expression.Call(ilikeMethod, efFunctions, arg2, arg5);
//        MethodCallExpression left3 = Expression.Call(ilikeMethod, efFunctions, arg, arg5);
//        MethodCallExpression left4 = Expression.Call(ilikeMethod, efFunctions, arg3, arg5);
//        MethodCallExpression right = Expression.Call(ilikeMethod, efFunctions, arg4, arg5);
//        return Expression.Or(left2, Expression.Or(left3, Expression.Or(left4, right)));
//    }

//    //public static IQueryable<T> Sort<T>(this IQueryable<T> queryable, DataQueryRequest query)
//    //{
//    //    var sortData = new
//    //    {
//    //        PropertyName = query.SortPropName,
//    //        Direction = query.SortDirection
//    //    };
//    //    if (sortData.PropertyName == null)
//    //    {
//    //        return queryable;
//    //    }

//    //    PropertyInfo propertyInfo = typeof(T).GetProperties().FirstOrDefault((PropertyInfo x) => x.Name.Equals(sortData.PropertyName, StringComparison.InvariantCultureIgnoreCase));
//    //    if ((object)propertyInfo == null)
//    //    {
//    //        throw new Exception(sortData.PropertyName + " named property not found");
//    //    }

//    //    ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "x");
//    //    Expression<Func<T, object>> expression = (Expression<Func<T, object>>)Expression.Lambda(Expression.Convert(Expression.MakeMemberAccess(parameterExpression, propertyInfo), typeof(object)), parameterExpression);
//    //    return queryable.Provider.CreateQuery<T>(Expression.Call(typeof(Queryable), (sortData.Direction == SortDirection.Ascending) ? "OrderBy" : "OrderByDescending", new Type[2]
//    //    {
//    //        queryable.ElementType,
//    //        typeof(object)
//    //    }, queryable.Expression, expression));
//    //}

//    public static IQueryable<T> Page<T>(this IQueryable<T> queryable, DataQueryRequest query)
//    {
//        var anon = new { query.Skip, query.Take };
//        if (anon.Skip == -1 || anon.Take == -1)
//        {
//            return queryable;
//        }

//        return queryable.Skip(anon.Skip).Take(anon.Take);
//    }

//    public static IQueryable<TOutput> LeftJoin2<TLeft, TRight, TKey, TOutput>(this IQueryable<TLeft> left, IEnumerable<TRight> right, Expression<Func<TLeft, TKey>> leftKey, Expression<Func<TRight, TKey>> rightKey, Expression<Func<TLeft, TRight?, TOutput>> join)
//    {
//        ParameterExpression parameterExpression = Expression.Parameter(typeof(LeftJoinInternal<TLeft, TRight>));
//        ParameterExpression parameterExpression2 = Expression.Parameter(typeof(TRight));
//        Expression<Func<LeftJoinInternal<TLeft, TRight>, TRight, TOutput>> resultSelector = Expression.Lambda<Func<LeftJoinInternal<TLeft, TRight>, TRight, TOutput>>(Expression.Invoke(join, Expression.Field(parameterExpression, "L"), parameterExpression2), new ParameterExpression[2] { parameterExpression, parameterExpression2 });
//        return left.GroupJoin(right, leftKey, rightKey, (TLeft l, IEnumerable<TRight> r) => new LeftJoinInternal<TLeft, TRight>
//        {
//            L = l,
//            R = r
//        }).SelectMany<LeftJoinInternal<TLeft, TRight>, TRight, TOutput>((LeftJoinInternal<TLeft, TRight> j) => j.R.DefaultIfEmpty(), resultSelector);
//    }
//}
