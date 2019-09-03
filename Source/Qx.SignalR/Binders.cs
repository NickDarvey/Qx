using Qx.Prelude;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using static Qx.Rewriters.BindingRewriter;

namespace Qx.SignalR
{
    /// <summary>
    /// A collection of functions for runtime binding to SignalR bits.
    /// </summary>
    internal static class Binders
    {
        /// <summary>
        /// Tries to binds methods to a set of parameters by name.
        /// </summary>
        /// <param name="parameterMethodBindings">If success, the resulting bindings.</param>
        /// <param name="errors">If failure, the errors which caused the failure.</param>
        /// <returns>True, if success. False, if failure</returns>
        public static bool TryBindMethods<TSourceDescription>(
            IEnumerable<ParameterExpression> parameters,
            IReadOnlyDictionary<string, TSourceDescription> nameMethodBindings,
            [NotNullWhen(true)] out IReadOnlyDictionary<ParameterExpression, TSourceDescription>? parameterMethodBindings,
            [NotNullWhen(false)] out IEnumerable<string>? errors)
        {
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

        /// <summary>
        /// Tries to binds methods to a set of parameters by name.
        /// </summary>
        public static Validation<string, IReadOnlyDictionary<ParameterExpression, TSourceDescription>> BindMethods<TSourceDescription>(
            IEnumerable<ParameterExpression> parameters,
            IReadOnlyDictionary<string, TSourceDescription> nameMethodBindings) =>
            TryBindMethods(parameters, nameMethodBindings, out var parameterMethodBindings, out var errors)
                ? new Validation<string, IReadOnlyDictionary<ParameterExpression, TSourceDescription>>(parameterMethodBindings)
                : new Validation<string, IReadOnlyDictionary<ParameterExpression, TSourceDescription>>(errors);


        /// <summary>
        /// Tries to convert lambda bindings to invocation (factory) bindings, injecting optional synthetic parameters if needed.
        /// </summary>
        /// <remarks>
        /// The results can be used as bindings when rewriting a Qx query to replace its unbound parameters,
        /// <see cref="Rewriters.Rewrite(Expression, IReadOnlyDictionary{ParameterExpression, Rewriters.InvocationFactory})"/>.
        /// </remarks>
        /// <param name="parameterLambdaBindings">A set of unbound <see cref="ParameterExpression"/> and <see cref="LambdaExpression"/> bindings.</param>
        /// <param name="optionalSyntheticParameters">Optional synthetic parameters to supply should a <see cref="LambdaExpression"/> require them.</param>
        /// <param name="parameterInvocationBindings">If success, the resulting bindings.</param>
        /// <param name="errors">If failure, the errors which caused the failure.</param>
        /// <returns>True, if success. False, if failure</returns>
        public static bool TryBindInvocations(
            IReadOnlyDictionary<ParameterExpression, LambdaExpression> parameterLambdaBindings,
            IEnumerable<ParameterExpression> optionalSyntheticParameters,
            [NotNullWhen(true)] out IReadOnlyDictionary<ParameterExpression, InvocationFactory>? parameterInvocationBindings,
            [NotNullWhen(false)] out IEnumerable<string>? errors)
        {
            var bindings_ = new Dictionary<ParameterExpression, InvocationFactory>();
            var errors_ = default(List<string>);
            foreach (var binding in parameterLambdaBindings)
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
                parameterInvocationBindings = default;
                errors = errors_;
                return false;
            }

            else
            {
                parameterInvocationBindings = bindings_;
                errors = default;
                return true;
            }
        }

        /// <summary>
        /// Tries to convert lambda bindings to invocation (factory) bindings, injecting optional synthetic parameters if needed.
        /// </summary>
        /// <remarks>
        /// The results can be used as bindings when rewriting a Qx query to replace its unbound parameters,
        /// <see cref="Rewriters.Rewrite(Expression, IReadOnlyDictionary{ParameterExpression, Rewriters.InvocationFactory})"/>.
        /// </remarks>
        /// <param name="optionalSyntheticParameters">Optional synthetic parameters to supply should a <see cref="LambdaExpression"/> require them.</para
        public static Validation<string, IReadOnlyDictionary<ParameterExpression, InvocationFactory>> BindInvocations(
            IReadOnlyDictionary<ParameterExpression, LambdaExpression> lambdaBindings,
            IEnumerable<ParameterExpression> optionalSyntheticParameters) =>
            TryBindInvocations(lambdaBindings, optionalSyntheticParameters, out var bindings, out var errors)
                ? new Validation<string, IReadOnlyDictionary<ParameterExpression, InvocationFactory>>(bindings)
                : new Validation<string, IReadOnlyDictionary<ParameterExpression, InvocationFactory>>(errors);



        /// <summary>
        /// Binds methods to lambda expressions so they can be invoked.
        /// </summary>
        public static IReadOnlyDictionary<ParameterExpression, LambdaExpression> BindLambdas<TSourceDescription>(
            IReadOnlyDictionary<ParameterExpression, TSourceDescription> parameterMethodBindings)
            where TSourceDescription : IQueryableSourceDescription =>
            parameterMethodBindings.ToDictionary(
                keySelector: b => b.Key,
                elementSelector: b =>
                {
                    var parameters = b.Value.Method.GetParameters();
                    var args = new ParameterExpression[parameters.Length];
                    for (int i = 0; i < args.Length; i++) args[i] = Expression.Parameter(parameters[i].ParameterType, parameters[i].Name);
                    var call = Expression.Call(Expression.Constant(b.Value.Instance), b.Value.Method, args);
                    return Expression.Lambda(call, args);
                });
    }
}