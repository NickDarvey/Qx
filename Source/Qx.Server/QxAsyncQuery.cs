using System;
using System.Collections.Generic;
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
    }
}
