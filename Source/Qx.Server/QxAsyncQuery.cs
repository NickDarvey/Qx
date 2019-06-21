using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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
        public static Expression ConvertQueryableElements<TElement>(Expression query)
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

        public static Expression ConvertValueTaskResult<TResult>(Expression query)
        {
            if (query.Type.IsGenericType == false ||
               query.Type.GetGenericTypeDefinition() != typeof(ValueTask<>))
                throw new ArgumentException($"Expected a query with type '{typeof(ValueTask<>).Name}' but got one with type '{query.Type}'");

            var sourceType = query.Type.GenericTypeArguments.Single();


            var asTaskMethod = query.Type.GetMethod(nameof(ValueTask<object>.AsTask));
            var taskType = asTaskMethod.ReturnType;

            var continuationArg1Type = taskType;
            var continuationResultType = typeof(TResult);
            var continuationType = typeof(Func<,>).MakeGenericType(continuationArg1Type, continuationResultType /* this is actually open */);
            var continueWithMethod = taskType.GetMethods()
                .Where(m => m.IsGenericMethod
                    && m.GetGenericArguments().Length == 1
                    && m.GetParameters().Length == 1
                    && m.GetParameters().Single().ParameterType == typeof(Func<,>).MakeGenericType(continuationArg1Type, m.GetGenericArguments().Single()))
                .Single()
                .MakeGenericMethod(continuationResultType);

            var continuationArg1Parameter = Expression.Parameter(continuationArg1Type);


            return Expression.Call(
                Expression.Call(query, asTaskMethod),
                continueWithMethod,
                Expression.Lambda(
                    Expression.Convert(
                        Expression.Property(continuationArg1Parameter, nameof(Task<object>.Result)),
                        continuationResultType),
                    continuationArg1Parameter)
                );

        }

        /// <summary>
        /// Compiles an <seealso cref="IAsyncQueryable{T}"/> query.
        /// </summary>
        /// <typeparam name="TElement">The type of the elements in the query.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>A function which will return the queryable when invoked.</returns>
        public static Func<IAsyncQueryable<TElement>> CompileQuery<TElement>(Expression query) =>
            Expression.Lambda<Func<IAsyncQueryable<TElement>>>(query).Compile();


        public static Func<CancellationToken, IAsyncQueryable<TElement>> CompileQuery2<TElement>(Expression query) =>
            Expression.Lambda<Func<CancellationToken, IAsyncQueryable<TElement>>>(query).Compile();
    }
}
