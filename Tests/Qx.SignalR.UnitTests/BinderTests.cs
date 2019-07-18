using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Qx.SignalR.UnitTests
{
    public class BinderTests
    {
        [Fact]
        public void TryBindInvocations_should_convert_exact_parameter_type_matches_exactly()
        {
            var param = Expression.Parameter(typeof(Func<int, int>));
            Expression<Func<int, int>> lambda = i => 0 + i;
            var args = new[] { Expression.Constant(1) };
            var lambdaBindings = new Dictionary<ParameterExpression, LambdaExpression> { { param, lambda } };

            var isBound = Binders.TryBindInvocations(lambdaBindings, Enumerable.Empty<ParameterExpression>(), out var invocationBindings, out var errors);

            Assert.True(isBound);
            Assert.Null(errors);
            Assert.Equal(lambdaBindings.Keys, invocationBindings.Keys);
            Assert.Equal(lambdaBindings.Values, invocationBindings.Values.Select(create => create(args).Expression));
        }

        [Fact]
        public void TryBindInvocations_should_append_synthetic_parameters_types()
        {
            var syntheticParameters = new[] { Expression.Parameter(typeof(bool)), Expression.Parameter(typeof(bool)) };
            var param = Expression.Parameter(typeof(Func<int, int>)); // Our unbound param doesn't know about the synthetic params
            Expression<Func<int, bool, bool, int>> lambda = (i, _, __) => 0 + i; // Our implementation depends on (well, discards) the synthetic params
            var args = new[] { Expression.Constant(1) }; // Our runtime arguments (non-synthetic, our synthetic args would be supplied later)
            var lambdaBindings = new Dictionary<ParameterExpression, LambdaExpression> { { param, lambda } };

            var isBound = Binders.TryBindInvocations(lambdaBindings, syntheticParameters, out var invocationBindings, out var errors);

            Assert.True(isBound);
            Assert.Null(errors);
            Assert.Equal(lambdaBindings.Keys, invocationBindings.Keys);
            Assert.Equal(lambdaBindings.Values, invocationBindings.Values.Select(create => create(args).Expression));
        }

        [Fact]
        public void TryBindInvocations_should_return_error_when_synthetic_parameters_types_are_out_of_order()
        {
            var syntheticParameters = new[] { Expression.Parameter(typeof(bool)), Expression.Parameter(typeof(string)) }; // bool, string
            var param = Expression.Parameter(typeof(Func<int, int>));
            Expression<Func<int, string, bool, int>> lambda = (i, _, __) => 0 + i; // string, bool
            var args = new[] { Expression.Constant(1) };
            var lambdaBindings = new Dictionary<ParameterExpression, LambdaExpression> { { param, lambda } };

            var isBound = Binders.TryBindInvocations(lambdaBindings, syntheticParameters, out var invocationBindings, out var errors);

            Assert.False(isBound);
            Assert.Null(invocationBindings);
            Assert.NotNull(errors);
            Assert.NotEmpty(errors);
            // TODO: String constant errors
        }

        [Fact]
        public void TryBindInvocations_should_error_when_synthetic_parameters_are_missing()
        {
            var syntheticParameters = new[] { Expression.Parameter(typeof(bool)) }; // one synthetic param
            var param = Expression.Parameter(typeof(Func<int, int>));
            Expression<Func<int, bool, string, int>> lambda = (i, _, __) => 0 + i; // two synthetic params
            var args = new[] { Expression.Constant(1) };
            var lambdaBindings = new Dictionary<ParameterExpression, LambdaExpression> { { param, lambda } };

            var isBound = Binders.TryBindInvocations(lambdaBindings, syntheticParameters, out var invocationBindings, out var errors);

            Assert.False(isBound);
            Assert.Null(invocationBindings);
            Assert.NotNull(errors);
            Assert.NotEmpty(errors);
            // TODO: String constant errors
        }
    }
}
