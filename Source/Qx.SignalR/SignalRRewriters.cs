using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Qx
{
    /// <summary>
    /// A collection of functions for rewrite expression trees.
    /// </summary>
    internal static class SignalRRewriters
    {
        /// <summary>
        /// Rewrites an <see cref="Expression"/> which returns a <see cref="IAsyncQueryable{T}"/> or <see cref="Task{IAsyncQueryable{T}}"/>
        /// to return a <see cref="IAsyncQueryable{object}"/> of <see cref="object"/> or a <see cref="Task{IAsyncQueryable{object}}"/> of <see cref="object"/>.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns>An expression which returns a <seealso cref="Task{object}"/></returns>
        public static Expression RewriteManyResultsType(Expression expression)
        {
            if (expression.Type.IsGenericType == false
                || expression.Type.GetGenericTypeDefinition() != typeof(IAsyncQueryable<>))
                throw new ArgumentException($"Expected a query with type '{typeof(IAsyncQueryable<>).Name}' but got one with type '{expression.Type}'");

            var sourceType = expression.Type.GenericTypeArguments.Single();
            var resultType = typeof(object);
            var elementParameter = Expression.Parameter(sourceType);
            var selectBody = Expression.Lambda(Expression.Convert(elementParameter, resultType), elementParameter);
            var selectMethod = Expression.Call(
                // TODO: Cache methodinfo per type
                method: new Func<IAsyncQueryable<object>, Expression<Func<object, object>>, IAsyncQueryable<object>>(AsyncQueryable.Select).GetMethodInfo().GetGenericMethodDefinition().MakeGenericMethod(sourceType, resultType),
                arg0: expression, arg1: selectBody);

            return selectMethod;
        }

        /// <summary>
        /// Rewrites an <seealso cref="Expression"/> which returns a <seealso cref="Task{T}"/> or <seealso cref="ValueTask{T}"/>
        /// to return a <seealso cref="Task{object}"/>.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns>An expression which returns a <seealso cref="Task{object}"/></returns>
        public static Expression RewriteSingleResultsType(Expression expression)
        {
            if (expression.Type.IsGenericType == false
                || expression.Type.GetGenericTypeDefinition() != typeof(ValueTask<>))
                throw new ArgumentException($"Expected a query with type '{typeof(ValueTask<>).Name}' but got one with type '{expression.Type}'");

            var sourceType = expression.Type.GenericTypeArguments.Single();


            var asTaskMethod = expression.Type.GetMethod(nameof(ValueTask<object>.AsTask));
            var taskType = asTaskMethod.ReturnType;

            var continuationArg1Type = taskType;
            var continuationResultType = typeof(object);
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
                Expression.Call(expression, asTaskMethod),
                continueWithMethod,
                Expression.Lambda(
                    Expression.Convert(
                        Expression.Property(continuationArg1Parameter, nameof(Task<object>.Result)),
                        continuationResultType),
                    continuationArg1Parameter)
                );
        }
        //public override Expression Visit(Expression node)
        //{
        //    if (node.Type)
        //        return base.Visit(node);
        //}
    }
}
