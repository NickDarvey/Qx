using System;
using System.Collections.Generic;
using System.Linq;

namespace Qx.Prelude
{
    public static class ValidationExtensions
    {
        public static Validation<TError, IEnumerable<TValue>> TraverseA<T, TError, TValue>(this IEnumerable<T> values, Func<T, Validation<TError, TValue>> func)
        {
            static Validation<TError, IEnumerable<TValue>> Init() =>
                new Validation<TError, IEnumerable<TValue>>(Enumerable.Empty<TValue>());

            static Validation<TError, Func<IEnumerable<TValue>, Func<TValue, IEnumerable<TValue>>>> Append() =>
                new Validation<TError, Func<IEnumerable<TValue>, Func<TValue, IEnumerable<TValue>>>>(xs => x => xs.Append(x));

            return values.Aggregate(seed: Init(), func: (s, x) => func(x).Apply(s.Apply(Append())));
        }
    }
}
