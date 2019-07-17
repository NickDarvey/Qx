using System;
using System.Collections.Generic;
using System.Linq;

namespace Qx.Fx
{
    internal static class Prelude
    {
        public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight value) => new Either<TLeft, TRight>(value);
        public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft value) => new Either<TLeft, TRight>(value);

        public static Either<IEnumerable<TLeft>, IEnumerable<TRight>> Sequence<TLeft, TRight>(this IEnumerable<Either<TLeft, TRight>> eithers) =>
            eithers.Aggregate(
                seed: new Either<IEnumerable<TLeft>, IEnumerable<TRight>>(Enumerable.Empty<TRight>()),
                func: (s, x) => s.Match(
                    right: s => x.Match(
                        right: x => new Either<IEnumerable<TLeft>, IEnumerable<TRight>>(s.Append(x)),
                        left: x => new Either<IEnumerable<TLeft>, IEnumerable<TRight>>(new [] { x })),
                    left: s => x.Match(
                        right: x => new Either<IEnumerable<TLeft>, IEnumerable<TRight>>(s),
                        left: x => new Either<IEnumerable<TLeft>, IEnumerable<TRight>>(s.Append(x))
                )));

        // TODO: Bottom exception if not set?
        internal struct Either<TLeft, TRight>
        {
            private readonly TLeft LeftValue;
            private readonly TRight RightValue;
            public bool IsRight { get; }
            public bool IsLeft { get => !IsRight; }

            public Either(TRight value)
            {
                RightValue = value;
                LeftValue = default;
                IsRight = true;
            }

            public Either(TLeft value)
            {
                RightValue = default;
                LeftValue = value;
                IsRight = false;
            }


            public Either<TLeft, TReturn> Select<TReturn>(Func<TRight, TReturn> select) => IsRight ? new Either<TLeft, TReturn>(select(RightValue)) : new Either<TLeft, TReturn>(LeftValue);

            public Either<TLeft, TReturn> SelectMany<TReturn>(Func<TRight, Either<TLeft, TReturn>> bind) => IsRight ? bind(RightValue) : new Either<TLeft, TReturn>(LeftValue);

            public TReturn Match<TReturn>(Func<TRight, TReturn> right, Func<TLeft, TReturn> left) => IsRight ? right(RightValue) : left(LeftValue);

        }
    }
    }


