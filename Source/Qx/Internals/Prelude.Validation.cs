using System;
using System.Collections.Generic;
using System.Linq;

namespace Qx.Internals
{
    public static partial class Prelude
    {
        public static Validation<TLeft, IEnumerable<TRight>> TraverseA<T, TLeft, TRight>(this IEnumerable<T> values, Func<T, Validation<TLeft, TRight>> func)
        {
            static Validation<TLeft, Func<IEnumerable<TRight>, Func<TRight, IEnumerable<TRight>>>> Append() =>
                new Validation<TLeft, Func<IEnumerable<TRight>, Func<TRight, IEnumerable<TRight>>>>(xs => x => xs.Append(x));

            return values.Aggregate(seed: new Validation<TLeft, IEnumerable<TRight>>(Enumerable.Empty<TRight>()),
                                    func: (s, x) => func(x).Apply(s.Apply(Append())));
        }

        // TODO: Bottom exception if not set?
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TError"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        public readonly struct Validation<TError, TValue>
        {
            private IEnumerable<TError> Errors { get; }
            private TValue Value { get; }
            public bool IsValid { get; }
            public bool IsInvalid { get => !IsValid; }

            public Validation(TValue value)
            {
                Value = value;
                Errors = default!;
                IsValid = true;
            }

            public Validation(IEnumerable<TError> errors)
            {
                Value = default!;
                Errors = errors;
                IsValid = false;
            }

            public Validation(params TError[] errors)
                : this(errors.AsEnumerable()) { }


            public Validation<TError, TReturnValue> Apply<TReturnValue>(Validation<TError, Func<TValue, TReturnValue>> func) =>
                (func.IsValid, IsValid) switch
                {
                    (true, true) => new Validation<TError, TReturnValue>(func.Value(Value)),
                    (true, false) => new Validation<TError, TReturnValue>(Errors),
                    (false, true) => new Validation<TError, TReturnValue>(func.Errors),
                    (false, false) => new Validation<TError, TReturnValue>(func.Errors.Concat(Errors)),
                };

            public Validation<TError, TReturnValue> Select<TReturnValue>(Func<TValue, TReturnValue> project) =>
                IsValid ? new Validation<TError, TReturnValue>(project(Value)) : new Validation<TError, TReturnValue>(Errors);

            public Validation<TError, TReturnValue> SelectMany<TReturnValue>(Func<TValue, Validation<TError, TReturnValue>> bind) =>
                IsValid ? bind(Value) : new Validation<TError, TReturnValue>(Errors);

            public Validation<TError, TReturnValue> SelectMany<TBoundValue, TReturnValue>(Func<TValue, Validation<TError, TBoundValue>> bind, Func<TValue, TBoundValue, TReturnValue> project)
            {
                var bound = SelectMany(bind);
                return bound.IsValid
                    ? new Validation<TError, TReturnValue>(project(Value, bound.Value))
                    : new Validation<TError, TReturnValue>(bound.Errors);
            }

            public TReturnValue Match<TReturnValue>(Func<TValue, TReturnValue> Valid, Func<IEnumerable<TError>, TReturnValue> Invalid) =>
                IsValid ? Valid(Value) : Invalid(Errors);

            public void Match(Action<TValue> Valid, Action<IEnumerable<TError>> Invalid)
            {
                if (IsValid) Valid(Value);
                else Invalid(Errors);
            }

        }
    }
}
