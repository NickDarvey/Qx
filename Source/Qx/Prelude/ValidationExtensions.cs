using System;
using System.Collections.Generic;
using System.Linq;

namespace Qx.Prelude
{
    public static class ValidationExtensions
    {
        public static Validation<TLeft, IEnumerable<TRight>> TraverseA<T, TLeft, TRight>(this IEnumerable<T> values, Func<T, Validation<TLeft, TRight>> func)
        {
            static Validation<TLeft, IEnumerable<TRight>> Init() =>
                new Validation<TLeft, IEnumerable<TRight>>(Enumerable.Empty<TRight>());

            static Validation<TLeft, Func<IEnumerable<TRight>, Func<TRight, IEnumerable<TRight>>>> Append() =>
                new Validation<TLeft, Func<IEnumerable<TRight>, Func<TRight, IEnumerable<TRight>>>>(xs => x => xs.Append(x));

            return values.Aggregate(seed: Init(), func: (s, x) => func(x).Apply(s.Apply(Append())));
        }
    }
}
