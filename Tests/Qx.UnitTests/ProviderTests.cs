using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Qx.UnitTests
{
    public class RewriterTests
    {
        private class TestKnownAsyncQueryable<T> : IAsyncQueryable<T>, IAsyncQueryProvider
        {
            public TestKnownAsyncQueryable(string name) => Expression = Expression.Parameter(typeof(IAsyncQueryable<T>), name);
            public TestKnownAsyncQueryable(Expression expression) => Expression = expression;

            public Type ElementType => typeof(T);
            public Expression Expression { get; }
            public IAsyncQueryProvider Provider => this;
            public IAsyncQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestKnownAsyncQueryable<TElement>(expression);
            public ValueTask<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken token) => throw new NotImplementedException();
            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        [Fact]
        public void Should_rewrite_simple_parameter_expression_to_invocation()
        {
            Expression<Func<IAsyncQueryable<int>>> range = () => AsyncEnumerable.Range(0, 50).AsAsyncQueryable();
            var factories = new Dictionary<string, Expression> { { "Range", range } };
            var client = new QxClientBase(new QxAsyncQueryProviderBase());
            var query = client.GetEnumerable<int>("Range");

            var result = new KnownAsyncQueryableRewriter(factories).Visit(query.Expression);

            Assert.Equal(ExpressionType.Invoke, result.NodeType);
            Assert.Equal(range, ((InvocationExpression)result).Expression);
        }

        [Fact]
        public void Should_rewrite_multiple_parameter_expressions_to_invocations()
        {
            Expression<Func<IAsyncQueryable<int>>> range1 = () => AsyncEnumerable.Range(0, 50).AsAsyncQueryable();
            Expression<Func<IAsyncQueryable<int>>> range2 = () => AsyncEnumerable.Range(50, 50).AsAsyncQueryable();
            var factories = new Dictionary<string, Expression> { { "Range1", range1 }, { "Range2", range2 } };
            var client = new QxClientBase(new QxAsyncQueryProviderBase());
            var source1 = client.GetEnumerable<int>("Range1");
            var source2 = client.GetEnumerable<int>("Range2");
            var query = source1.Join(source2, x => x, y => y, (x, y) => x + y);

            var result = new KnownAsyncQueryableRewriter(factories).Visit(query.Expression);

            Assert.Equal(ExpressionType.Call, result.NodeType);
            var joinExpression = (MethodCallExpression)result;
            var joinExpressionArg1 = joinExpression.Arguments.First();
            var joinExpressionArg2 = joinExpression.Arguments.Skip(1).First();

            Assert.Equal(ExpressionType.Invoke, joinExpressionArg1.NodeType);
            Assert.Equal(range1, ((InvocationExpression)joinExpressionArg1).Expression);

            Assert.Equal(ExpressionType.Invoke, joinExpressionArg2.NodeType);
            Assert.Equal(range2, ((InvocationExpression)joinExpressionArg2).Expression);
        }


        [Fact]
        public void Should_rewrite_parameter_expressions_with_arguments_to_invocations()
        {
            Expression<Func<int, IAsyncQueryable<int>>> range = (count) => AsyncEnumerable.Range(0, count).AsAsyncQueryable();
            var factories = new Dictionary<string, Expression> { { "Range", range } };
            var client = new QxClientBase(new QxAsyncQueryProviderBase());
            var query = client.GetEnumerable<int, int>("Range")(10);



            var result = new KnownAsyncQueryableRewriter(factories).Visit(query.Expression);

            Assert.Equal(ExpressionType.Invoke, result.NodeType);
            Assert.Equal(range, ((InvocationExpression)(((LambdaExpression)(((InvocationExpression)result).Expression)).Body)).Expression);
        }

        [Fact]
        public async Task Should_evaluate()
        {
            Expression<Func<int, IAsyncQueryable<int>>> range = (count) => AsyncEnumerable.Range(0, count).AsAsyncQueryable();
            var factories = new Dictionary<string, Expression> { { "Range", range } };
            var client = new QxClientBase(new QxAsyncQueryProviderBase());
            var source = client.GetEnumerable<int, int>("Range")(10);
            var query = source.Join(source, x => x, y => y, (x, y) => x + y);

            var result = await Expression.Lambda<Func<IAsyncQueryable<int>>>(new KnownAsyncQueryableRewriter(factories).Visit(query.Expression)).Compile()().ToArrayAsync();

            Assert.Equal(new[] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18 }, result);
        }

    }

    //public static class Qx
    //{
    //    /// <summary>
    //    /// Binds a query 
    //    /// </summary>
    //    /// <typeparam name="TSource"></typeparam>
    //    /// <typeparam name="TElement"></typeparam>
    //    /// <param name="sources"></param>
    //    /// <param name="query"></param>
    //    /// <returns></returns>
    //    public static IAsyncEnumerable<object> ApplyQuery(IReadOnlyDictionary<string, Expression<Func<IAsyncQueryable<object>>>> sources, Expression query)
    //    {
    //        var x = default(IAsyncQueryable<int>);
    //        x.exp
    //        //IAsyncQueryable -> IAsyncEnumerable

    //        // so i should implement a provider?

    //        // It would be polite if the expression i received had something like
    //        // q1, q2 => q1.Join(q2, x=>x)
    //    }
    //}

    // Purpose is to go from 'known' queryables to actual queryables (be it AsyncEnumerableQueryables or something else)
    //public class KnownAsyncQueryable<T> : 
    //    public Type ElementType => throw new NotImplementedException();

    //    public Expression Expression => throw new NotImplementedException();

    //    public IAsyncQueryProvider Provider => throw new NotImplementedException();

    //    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    //    {
    //        // We've got an expression with ParameterExpressions in the place of IAsyncQueryables
    //        throw new NotImplementedException();
    //    }
    //}




    public class KnownAsyncQueryableProvider : IAsyncQueryProvider
    {
        private readonly IReadOnlyDictionary<string, Expression> _queryableFactories;

        public KnownAsyncQueryableProvider(IReadOnlyDictionary<string, Expression> queryableFactories)
        {
            _queryableFactories = queryableFactories;
        }


        public IAsyncQueryable<TElement> CreateQuery<TElement>(Expression expression) => new KnownAsyncQueryable<TElement>(expression);

        public ValueTask<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        private class KnownAsyncQueryable<T> : IOrderedAsyncQueryable<T>
        {
            private readonly Expression _expression;

            public KnownAsyncQueryable(Expression expression) => _expression = expression;
            public Type ElementType => throw new NotImplementedException();

            public Expression Expression => throw new NotImplementedException();

            public IAsyncQueryProvider Provider => throw new NotImplementedException();

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }



    /// <summary>
    /// A queryable which references known AsyncQueryables by name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KnownAsyncQueryableQuery<T> : IOrderedAsyncQueryable<T>, IAsyncQueryProvider
    {
        private readonly IReadOnlyDictionary<string, Expression> _queryableFactories;
        private readonly Expression _expression;

        public KnownAsyncQueryableQuery(IReadOnlyDictionary<string, Expression> queryableFactories, Expression expression)
        {
            _queryableFactories = queryableFactories;
            _expression = expression;
        }

        Type IAsyncQueryable.ElementType => typeof(T);

        Expression IAsyncQueryable.Expression => _expression;

        IAsyncQueryProvider IAsyncQueryable.Provider => this;

        IAsyncQueryable<TElement> IAsyncQueryProvider.CreateQuery<TElement>(Expression expression) => new KnownAsyncQueryableQuery<TElement>(_queryableFactories, expression);

        ValueTask<TResult> IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var expression = Expression.Lambda<Func<IAsyncQueryable<T>>>(new KnownAsyncQueryableRewriter(_queryableFactories).Visit(_expression), null);
            var queryable = expression.Compile()();
            return queryable.GetAsyncEnumerator(cancellationToken);
        }
    }

    /// <summary>
    /// Lowers a KnownAsyncQueryable expression to an AsyncQueryable expression.
    /// </summary>
    public class KnownAsyncQueryableRewriter : ExpressionVisitor
    {
        private readonly IReadOnlyDictionary<string, Expression> _queryableFactories;

        public KnownAsyncQueryableRewriter(IReadOnlyDictionary<string, Expression> queryableFactories)
        {
            _queryableFactories = queryableFactories;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            // TODO: Check if parameter is unbound
            // TODO: Check if types in parameter match the types in our factory

            if (TryGetDelegateType(node.Type, out var _, out var type) && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncQueryable<>))
            {
                if (_queryableFactories.TryGetValue(node.Name, out var factory)) return factory;
                else throw new InvalidOperationException($"No known queryable named '{node.Name}'");
            }

            else return node;
        }

        private static bool TryGetDelegateType(Type type, out Type[] parameterTypes, out Type returnType)
        {
            if (type != (Type)null && typeof(Delegate).IsAssignableFrom(type))
            {
                var method = type.GetMethod("Invoke");
                parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray<Type>();
                returnType = method.ReturnType;
                return true;
            }
            parameterTypes = null;
            returnType = null;
            return false;
        }
    }

    public interface IQxExpressible
    {
        public Expression Expression { get; }
        public IAsyncQueryProvider Provider { get; }
    }

    //public interface IQxAsyncQueryable<T> : IAsyncQueryable<T>, IQxExpressible { }

    public interface IQxClient
    {
        IAsyncQueryable<TElement> GetEnumerable<TElement>(string name);
        Func<TArg, IAsyncQueryable<TElement>> GetEnumerable<TArg, TElement>(string name);
    }

    public class QxAsyncQueryProviderBase : IAsyncQueryProvider
    {
        public IAsyncQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new QxAsyncQueryable<TElement>(this, expression);

        public ValueTask<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        internal IAsyncEnumerator<TElement> GetAsyncEnumerator<TElement>(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    public class QxAsyncQueryable<T> : IAsyncQueryable<T>
    {
        private readonly QxAsyncQueryProviderBase _provider;
        private readonly Expression _expression;

        public QxAsyncQueryable(QxAsyncQueryProviderBase provider, Expression expression)
        {
            _provider = provider;
            _expression = expression;
        }

        public Type ElementType => typeof(T);

        public Expression Expression => _expression;

        public IAsyncQueryProvider Provider => _provider;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => _provider.GetAsyncEnumerator<T>(cancellationToken);
    }

    public class QxClientBase : IQxClient
    {
        private readonly QxAsyncQueryProviderBase _queryProvider;

        public QxClientBase(QxAsyncQueryProviderBase queryProvider) => _queryProvider = queryProvider;

        public IAsyncQueryable<TElement> GetEnumerable<TElement>(string name)
        {
            var expression = Expression.Invoke(
                Expression.Parameter(typeof(Func<IAsyncQueryable<TElement>>), name));
            return _queryProvider.CreateQuery<TElement>(expression);
        }

        public Func<TArg, IAsyncQueryable<TElement>> GetEnumerable<TArg, TElement>(string name)
        {
            var arg1 = Expression.Parameter(typeof(TArg));
            return arg => _queryProvider.CreateQuery<TElement>(Expression.Invoke(
                Expression.Lambda<Func<TArg, IAsyncQueryable<TElement>>>(
                    Expression.Invoke(Expression.Parameter(typeof(Func<TArg, IAsyncQueryable<TElement>>), name), arg1), arg1),
                    Expression.Constant(arg, typeof(TArg))));
        }
    }

}
