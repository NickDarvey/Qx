using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public static async ValueTask<Validation<TError, TReturnValue>> SelectMany<TError, TValue, TReturnValue>(this ValueTask<Validation<TError, TValue>> @this, Func<TValue, ValueTask<Validation<TError, TReturnValue>>> bind) =>
            await (await @this).SelectMany(bind);

        public static async ValueTask<Validation<TError, TReturnValue>> SelectMany<TError, TValue, TBoundValue, TReturnValue>(this ValueTask<Validation<TError, TValue>> @this, Func<TValue, ValueTask<Validation<TError, TBoundValue>>> bind, Func<TValue, TBoundValue, TReturnValue> project) =>
            await (await @this).SelectMany(bind, project);

        public static async ValueTask<Validation<TError, TReturnValue>> Select<TError, TValue, TReturnValue>(this ValueTask<Validation<TError, TValue>> @this, Func<TValue, TReturnValue> project) =>
            (await @this).Select(project);
    }
}
