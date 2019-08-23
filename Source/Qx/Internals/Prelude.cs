using System;
using System.Collections.Generic;
using System.Text;

namespace Qx.Internals
{
    public static partial class Prelude
    {
        //public static R Apply<T, R>(this T value, Func<T, R> func) => func(value);
        //public static R Match<T, R>(this T? nullableValue, Func<T, R> value, Func<R> @null) where T : class =>
        //    nullableValue is null ? @null() : value(nullableValue);

        //public static R Match<T, R>(this T? nullableValue, Func<T, R> value, Func<R> @null) where T : struct =>
        //    nullableValue.HasValue ? value(nullableValue.Value) : @null();
    }
}
