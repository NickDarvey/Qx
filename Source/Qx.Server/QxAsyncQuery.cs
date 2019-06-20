using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Qx
{
    public static class QxAsyncQuery
    {
        /// <summary>
        /// Finds methods which returns the IAsyncQueryables on an object.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="nameSelector"></param>
        /// <returns>A dictionary with the name of the queryable and a lambda expression which returns the queryable when invoked.</returns>
        public static IReadOnlyDictionary<string, LambdaExpression> FindQueryables(object @this, Func<MethodInfo, string> nameSelector) =>
            @this.GetType().GetMethods()
            .Where(m => m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncQueryable<>))
            .ToDictionary(
                keySelector: nameSelector,
                elementSelector: m =>
                {
                    var args = m.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray(/* generate params once */);
                    var call = Expression.Call(Expression.Constant(@this), m, args);
                    return Expression.Lambda(call, args);
                });

        /// <summary>
        /// Converts each T element of an <seealso cref="IAsyncQueryable{T}"/> query to an <typeparamref name="TElement"/>.
        /// </summary>
        /// <returns>The converted <seealso cref="IAsyncQueryable{T}"/></returns>
        public static Expression ConvertQueryElements<TElement>(Expression query)
        {
            if (query.Type.IsGenericType == false ||
                query.Type.GetGenericTypeDefinition() != typeof(IAsyncQueryable<>))
                throw new ArgumentException($"Expected a query with type '{typeof(IAsyncQueryable<>).Name}' but got one with type '{query.Type}'");

            var sourceType = query.Type.GenericTypeArguments.Single();
            var resultType = typeof(TElement);
            var elementParameter = Expression.Parameter(sourceType);
            var selectBody = Expression.Lambda(Expression.Convert(elementParameter, resultType), elementParameter);
            var selectMethod = Expression.Call(
                // TODO: Cache methodinfo per type
                method: new Func<IAsyncQueryable<object>, Expression<Func<object, object>>, IAsyncQueryable<object>>(AsyncQueryable.Select).GetMethodInfo().GetGenericMethodDefinition().MakeGenericMethod(sourceType, resultType),
                arg0: query, arg1: selectBody);
            return selectMethod;
        }

        /// <summary>
        /// Compiles an <seealso cref="IAsyncQueryable{T}"/> query.
        /// </summary>
        /// <typeparam name="TElement">The type of the elements in the query.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>A function which will return the queryable when invoked.</returns>
        public static Func<IAsyncQueryable<TElement>> CompileQuery<TElement>(Expression query) =>
            Expression.Lambda<Func<IAsyncQueryable<TElement>>>(query).Compile();
    }
}
