using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qx.Client;
using Qx.Interfaces;
using Serialize.Linq.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace QxClient
{
    class Program
    {
        static async Task Main2(string[] args)
        {
            // Start by using SignalR as a transport n' protocol coz doing it myself looks scary

            IQxClient ctx = null;
            IQxservable<int> range = ctx.GetObservable<int>("random"); // reference cold observable, via proxy
            IQxserver<int> console = ctx.GetObserver<int>("console");
            // IQxserver console = // how do _I_ want to observe this? how do get a refernce to _me_
            // at the end of the day the server needs to materialize it as the connection to this client
            // though it could very well be referring to some other client

            //var local = range.ToAsyncObservable() 

            var subscription = await range.SubscribeAsync(console, "test");
            Console.WriteLine("Observing remotely");


            await Task.Delay(5000);

            await subscription.DisposeAsync();

            Console.WriteLine("Done");
        }

        static async Task Main(string[] args)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/qx")
                .ConfigureLogging(log => { log.AddConsole(); log.SetMinimumLevel(LogLevel.Debug); })
                .AddNewtonsoftJsonProtocol(s => s.PayloadSerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects)
                .Build();

            await Task.Delay(5000);
            await connection.StartAsync(); 

            var x = new MyQxClient(connection);

            var q = x.GetEnumerable<int>("Range").Where(n => n % 2 == 0);

            await q.ForEachAsync(n => Console.WriteLine("Hello you: " + n));
        }

        static async Task Main3(string[] args)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/random")
                .Build();

            await Task.Delay(5000);
            await connection.StartAsync();

            // Build a SignalR IAsyncQueryable provider
            await connection.StreamAsync<int>("Range", 0, 50)

                .AsAsyncQueryable()
                


                .Where(n => n % 2 == 0)
                .ForEachAsync(n => Console.WriteLine("Received: " + n));

            await connection.DisposeAsync();
        }
    }

    public class MyQxClient
    {
        private readonly HubConnection _connection;

        public MyQxClient(HubConnection connection)
        {
            _connection = connection;
        }

        public IAsyncQueryable<T> GetEnumerable<T>(string name)
        {
            return new QxQueryable<T>(_connection, Expression.Parameter(typeof(IAsyncQueryable<T>), name));
        }


    }

    //public class QxAsyncQueryableProvider<T> : IAsyncQueryProvider
    //{
    //    public IAsyncQueryable<TElement> CreateQuery<TElement>(Expression expression)
    //    {
    //        return new QxAsyncQueryable<TElement>(this, expression);
    //    }

    //    public ValueTask<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken token)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class QxAsyncQueryable<T> : IAsyncQueryable<T>
    //{
    //    public Type ElementType => throw new NotImplementedException();

    //    public Expression Expression { get; }

    //    public IAsyncQueryProvider Provider { get; }

    //    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public QxAsyncQueryable(IAsyncQueryProvider provider, Expression expression)
    //    {
    //        Provider = provider;
    //        Expression = expression;
    //    }
    //}

    public class QxQueryable<T> : IOrderedAsyncQueryable<T>, IAsyncQueryProvider
    {
        private readonly HubConnection _connection;
        private readonly Expression _expression;

        public QxQueryable(HubConnection connection, Expression expression) => (_connection, _expression) = (connection, expression);

        Type IAsyncQueryable.ElementType => typeof(T);

        Expression IAsyncQueryable.Expression => _expression;

        IAsyncQueryProvider IAsyncQueryable.Provider => this;

        IAsyncQueryable<TElement> IAsyncQueryProvider.CreateQuery<TElement>(Expression expression) => new QxQueryable<TElement>(_connection, expression);


        ValueTask<TResult> IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        // Actually this is kinda what we need to do on the server (visit params and replace with constants)

        //IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
        //{
        //    var param = (ParameterExpression)_expression;
        //    var enumerable = _connection.StreamAsync<T>(param.Name, cancellationToken);
        //    var param2 = Expression.Constant(enumerable, enumerable.GetType());

        //    var expression = Expression.Lambda<Func<IAsyncEnumerable<T>>>(param2, null);
        //    var result = expression.Compile()();
        //    return result.GetAsyncEnumerator(cancellationToken);
        //}


        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            var weightloss = new NodeFactory();
            var slim = weightloss.Create(_expression);

            var enumerable = _connection.StreamAsync<T>("qx", slim, cancellationToken);
            var param2 = Expression.Constant(enumerable, enumerable.GetType());

            var expression = Expression.Lambda<Func<IAsyncEnumerable<T>>>(param2, null);
            var result = expression.Compile()();
            return result.GetAsyncEnumerator(cancellationToken);
        }
    }

    //public class QxExpressionRewriter : ExpressionVisitor
    //{
    //    private readonly 
    //    protected override Expression VisitParameter(ParameterExpression node)
    //    {
    //        return base.VisitParameter(node);
    //    }

    //}
}
