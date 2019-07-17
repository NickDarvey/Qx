using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static Qx.Fx.Prelude;

namespace Qx
{
    /// <summary>
    /// A collection of functions for runtime binding to SignalR bits.
    /// </summary>
    internal static class SignalRBinders
    {
        /// <summary>
        /// Binds methods to a set of parameters by name.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="nameMethodBindings"></param>
        /// <param name="parameterMethodBindings"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static bool TryBindMethods<TSourceDescription>(IEnumerable<ParameterExpression> parameters, IReadOnlyDictionary<string, TSourceDescription> nameMethodBindings, out IReadOnlyDictionary<ParameterExpression, TSourceDescription> parameterMethodBindings, out IEnumerable<string> errors)
        {
            // TODO: Let's try a functional way of impl this, 'coz it'd be fun
            // nameMethodBindings -> parameters -> Either<Errors, Map<ParameterExpression, TDesc>>
            // parameters.Select(name => nameMethodBindings.TryGetValue(parameters.Name, out var method) ? Right(method) : Left("NoMethodFound"))
            var bindings_ = new Dictionary<ParameterExpression, TSourceDescription>();
            var errors_ = default(List<string>);
            foreach (var parameter in parameters)
            {
                // We don't test if the parameters match yet, because there could be synthetic parameters used,
                // we just ensure that such a method exists.
                if (nameMethodBindings.TryGetValue(parameter.Name, out var method))
                {
                    bindings_[parameter] = method;
                }
                else
                {
                    errors_ ??= new List<string>();
                    errors_.Add($"No hub method found for query source named '{parameter.Name}'");
                }
            }

            if (errors_?.Count > 0)
            {
                parameterMethodBindings = default;
                errors = errors_;
                return false;
            }

            else
            {
                parameterMethodBindings = bindings_;
                errors = default;
                return true;
            }
        }

        public static Either<IEnumerable<string>, IEnumerable<(ParameterExpression, TSourceDescription)>> TryBindMethods2<TSourceDescription>(IEnumerable<ParameterExpression> parameters, IReadOnlyDictionary<string, TSourceDescription> nameMethodBindings) =>
            parameters.Select(parameter => nameMethodBindings.TryGetValue(parameter.Name, out var method)
                ? Right<string, (ParameterExpression, TSourceDescription)>((parameter, method))
                : Left<string, (ParameterExpression, TSourceDescription)>($"Method not found"))
            .Sequence();

        /// <summary>
        /// Tries to converts lambda bindings to invocation (factory) bindings, injecting optional synthetic parameters if needed.
        /// </summary>
        /// <remarks>
        /// The results can be used as bindings when rewriting a Qx query to replace its unbound parameters,
        /// <see cref="Rewriters.Rewrite(Expression, IReadOnlyDictionary{ParameterExpression, Rewriters.InvocationFactory})"/>.
        /// </remarks>
        /// <param name="lambdaBindings">A set of unbound <see cref="ParameterExpression"/> and <see cref="LambdaExpression"/> bindings.</param>
        /// <param name="optionalSyntheticParameters">Optional synthetic parameters to supply should a <see cref="LambdaExpression"/> require them.</param>
        /// <param name="bindings">If success, the resulting bindings.</param>
        /// <param name="errors">If failure, the errors which caused the failure.</param>
        /// <returns>True, if success. False, if failure</returns>
        public static bool TryBindInvocations(IReadOnlyDictionary<ParameterExpression, LambdaExpression> lambdaBindings, IEnumerable<ParameterExpression> optionalSyntheticParameters, out IReadOnlyDictionary<ParameterExpression, Rewriters.InvocationFactory> bindings, out IEnumerable<string> errors)
        {
            var bindings_ = new Dictionary<ParameterExpression, Rewriters.InvocationFactory>();
            var errors_ = default(List<string>);
            foreach (var binding in lambdaBindings)
            {
                if (binding.Value == default) throw new InvalidOperationException($"No binding for query source named '{binding.Key}'");

                if (binding.Key.Type == binding.Value.Type)
                {
                    bindings_[binding.Key] = args => Expression.Invoke(binding.Value, args);
                }

                else // with synthetic params
                {
                    //if (binding.Key.Type.IsGenericType == false || binding.Key.Type.GetGenericTypeDefinition()) // TODO: Some kind of check to make sure we're actually dealing with a Func of whatever arity
                    var originalAndSyntheticParameterTypes = binding.Key.Type.GetGenericArguments().SkipLast(1).Concat(optionalSyntheticParameters.Select(p => p.Type));
                    var boundParameterTypes = binding.Value.Parameters.Select(p => p.Type);

                    if (originalAndSyntheticParameterTypes.SequenceEqual(boundParameterTypes) == false)
                    {
                        errors_ ??= new List<string>();
                        errors_.Add($"Specified parameters ({string.Join(", ", originalAndSyntheticParameterTypes)}) for query source named '{binding.Key.Name}' do not match the bound parameters ({string.Join(", ", boundParameterTypes)})");
                    }

                    bindings_[binding.Key] = args => Expression.Invoke(binding.Value, args.Concat(optionalSyntheticParameters));
                }
            }

            if (errors_?.Count > 0)
            {
                bindings = default;
                errors = errors_;
                return false;
            }

            else
            {
                bindings = bindings_;
                errors = default;
                return true;
            }
        }
    }
}