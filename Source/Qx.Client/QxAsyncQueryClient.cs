using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Qx
{
    public class QxAsyncQueryClient : IAsyncQueryClient
    {
        private readonly IAsyncQueryServiceProvider _service;

        public QxAsyncQueryClient(IAsyncQueryServiceProvider serviceProvider) =>
            _service = new NormalizingAsyncQueryServiceProvider(@base: serviceProvider);

        public IAsyncQueryable<TElement> GetEnumerable<TElement>(string name) =>
            GetEnumerable<TElement>(Expression.Parameter(typeof(Func<IAsyncQueryable<TElement>>), name));

        public Func<TArg, IAsyncQueryable<TElement>> GetEnumerable<TArg, TElement>(string name) =>
            arg => GetEnumerable<TElement>(
                Expression.Parameter(typeof(Func<TArg, IAsyncQueryable<TElement>>), name),
                Expression.Constant(arg, typeof(TArg)));

        public Func<TArg1, TArg2, IAsyncQueryable<TElement>> GetEnumerable<TArg1, TArg2, TElement>(string name) =>
            (arg1, arg2) => GetEnumerable<TElement>(
                Expression.Parameter(typeof(Func<TArg1, TArg2, IAsyncQueryable<TElement>>), name),
                Expression.Constant(arg1, typeof(TArg1)),
                Expression.Constant(arg2, typeof(TArg2)));

        private IAsyncQueryable<TElement> GetEnumerable<TElement>(ParameterExpression parameter, params Expression[] arguments) =>
            new QxAsyncQuery<TElement>(_service, Expression.Invoke(parameter, arguments));

        /// <summary>
        /// Normalizes expressions before passing them to the actual service provider.
        /// </summary>
        private class NormalizingAsyncQueryServiceProvider : IAsyncQueryServiceProvider
        {
            private static readonly IEnumerable<Func<Expression, Expression>> Normalizers = new Func<Expression, Expression>[]
            {
                expr => new ClientCallKnownResourceRewriter().Visit(expr),
            };

            private readonly IAsyncQueryServiceProvider _base;

            public NormalizingAsyncQueryServiceProvider(IAsyncQueryServiceProvider @base) => _base = @base;

            public IAsyncEnumerator<T> GetAsyncEnumerator<T>(Expression expression, CancellationToken token) =>
                _base.GetAsyncEnumerator<T>(Normalize(expression), token);

            public ValueTask<T> GetAsyncResult<T>(Expression expression, CancellationToken token) =>
                _base.GetAsyncResult<T>(Normalize(expression), token);

            private Expression Normalize(Expression expression) => Normalizers.Aggregate(expression, (s, x) => x(s));

            // TODO: (Optionally, opt-in?) support extensions of the client class, e.g. an extension which offers a strongly-typed Range method
            private class ClientCallKnownResourceRewriter : ExpressionVisitor
            {
                protected override Expression VisitMethodCall(MethodCallExpression node)
                {
                    var expression = base.VisitMethodCall(node);

                    // If the method call expression's object is our client
                    // and it's one of the 'Get' functions
                    // with one argument (the 'name')

                    if (!(expression is MethodCallExpression methodCallExpression)
                        || methodCallExpression.Object == null
                        || typeof(IAsyncQueryClient).IsAssignableFrom(methodCallExpression.Object.Type) == false
                        || methodCallExpression.Method.Name != nameof(IAsyncQueryClient.GetEnumerable)
                        || methodCallExpression.Arguments.Count != 1
                        || !(methodCallExpression.Arguments[0] is ConstantExpression argumentExpression)
                        || !(argumentExpression.Value is string name)
                        || TryGetKnownResourceType(methodCallExpression.Type, out var resourceType, out var isResourceInvocation) == false)
                        return expression;

                    if (isResourceInvocation) return Expression.Parameter(methodCallExpression.Type, name);
                    // TOTHINK: Do I really need to hoist it to a func? The execution side could do this if it needs it.
                    else return Expression.Invoke(Expression.Parameter(Expression.GetFuncType(methodCallExpression.Type), name));
                }
            }
        }

        // TODO: Extract
        private static bool TryGetKnownResourceType(Type type, out Type resourceType, out bool isResourceInvocation)
        {
            if (IsKnownResourceType(type))
            {
                resourceType = type;
                isResourceInvocation = false;
                return true;
            }

            if (TryGetDelegateTypes(type, out _, out var returnType)
                && IsKnownResourceType(returnType))
            {
                resourceType = returnType;
                isResourceInvocation = true;
                return true;
            }

            resourceType = default!;
            isResourceInvocation = default;
            return false;
        }

        // TODO: Extract
        private static bool IsKnownResourceType(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncQueryable<>);

        // TODO: Extract
        private static bool TryGetDelegateTypes(Type type, out Type[] parameterTypes, out Type returnType)
        {
            if (typeof(Delegate).IsAssignableFrom(type))
            {
                var methodInfo = type.GetMethod("Invoke");
                parameterTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
                returnType = methodInfo.ReturnType;
                return true;
            }

            parameterTypes = default!;
            returnType = default!;
            return false;
        }
    }
}
