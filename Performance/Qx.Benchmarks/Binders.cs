using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Qx.Benchmarks
{
    [MemoryDiagnoser]
    public class Binders
    {
        private static readonly ParameterExpression[] _parameters = new[]
        {
            Expression.Parameter(typeof(int), "Zero"),
            Expression.Parameter(typeof(long), "One"),
            Expression.Parameter(typeof(string), "Two"),
        };

        private static readonly Dictionary<string, string> _methods = new Dictionary<string, string>
        {
            { _parameters[0].Name, "0" },
            { _parameters[1].Name, "1" },
            { _parameters[2].Name, "2" },
        };

        [Benchmark(Baseline = true)]
        public IReadOnlyDictionary<ParameterExpression, string> TryBindMethods()
        {
            _ = Qx.SignalR.Binders.TryBindMethods(_parameters, _methods, out var parameterMethodBindings, out var errors);
            return parameterMethodBindings;
        }
    }
}
