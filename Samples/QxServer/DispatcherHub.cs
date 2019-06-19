using Microsoft.AspNetCore.SignalR;
using Qx;
using Serialize.Linq.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace QxServer
{
    public class QueryableHub : Hub
    {
        [HubMethodName("qx")]
        public IAsyncEnumerable<object> GetEnumerable(ExpressionNode expression)
        {
            // TODO: Checks for duplicates (overrides) and stuff?

            var queryables = this.GetType().GetMethods()
                .Where(m => m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncQueryable<>))
                .Select(m =>
                {
                    var args = m.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray(/* generate params once */);
                    var call = Expression.Call(Expression.Constant(this), m, args);
                    return (
                     Name: m.GetCustomAttribute<HubMethodNameAttribute>()?.Name ?? m.Name,
                     Expression: Expression.Lambda(call, args )
                     //Expression: Expression.Lambda(Expression.Call(Expression.Constant(this), m, m.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name))))
                     );
                })
                .ToDictionary(kv => kv.Name, kv => (Expression)kv.Expression);

            var expr = expression.ToExpression();
            var query = new AsyncQueryableRewriter(queryables).Visit(expr);
            var sourceType = query.Type.GenericTypeArguments[0];
            var resultType = typeof(object);
            var elementParameter = Expression.Parameter(sourceType);
            var selectBody = Expression.Lambda(Expression.Convert(elementParameter, resultType), elementParameter);
            var selectMethod = Expression.Call(
                method: new Func<IAsyncQueryable<object>, Expression<Func<object, object>>, IAsyncQueryable<object>>(AsyncQueryable.Select).GetMethodInfo().GetGenericMethodDefinition().MakeGenericMethod(sourceType, resultType),
                arg0: query, arg1: selectBody);
            var func = Expression.Lambda<Func<IAsyncQueryable<object>>>(selectMethod).Compile();

            return func();

        }
    }










    public class MyHub : QueryableHub
    {
        public IAsyncQueryable<int> Range(int start, int count) => AsyncEnumerable.Range(start, count).AsAsyncQueryable();
    }

}
