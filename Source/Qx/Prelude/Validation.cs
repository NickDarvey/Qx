using System;
using System.Collections.Generic;
using System.Linq;

namespace Qx.Prelude
{
    /// <summary>
    /// A structure representing either a valid result of <typeparamref name="TValue"/>,
    /// or a collection of errors of <typeparamref name="TError"/>.
    /// </summary>
    /// <remarks>
    /// If C# adds support for discriminated unions, this can go away.
    /// This exists because I couldn't find a neat way to express the return of functions which
    /// might return errors now that nullable reference types is a thing. I was using the
    /// Try(out result, out errors) pattern, but after enabling nullable reference types I needed
    /// to use the damnit operator everywhere with that pattern, kinda defeating the purpose.
    /// </remarks>
    /// <typeparam name="TError">The type of <see cref="Validation{TError, TValue}.Errors"/></typeparam>
    /// <typeparam name="TValue">The type of <see cref="Validation{TError, TValue}.Value"/></typeparam>
    public readonly struct Validation<TError, TValue>
    {
        private readonly IEnumerable<TError> _errors;

        private IEnumerable<TError> Errors { get => _errors ?? Enumerable.Empty<TError>(); } // Bottom (not initialized) returns empty errors
        private TValue Value { get; }
        public bool IsValid { get; }
        public bool IsInvalid { get => !IsValid; }

        public Validation(TValue value)
        {
            Value = value;
            _errors = default!;
            IsValid = true;
        }

        public Validation(IEnumerable<TError> errors)
        {
            Value = default!;
            _errors = errors;
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
