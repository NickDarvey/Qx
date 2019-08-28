//using System;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Text;

//namespace Qx
//{
//    public static class ComposeTryPattern
//    {
//        public static bool TryGet42(string input, [NotNullWhen(false)] out IEnumerable<string>? errors) =>
//            throw new NotImplementedException();

//        public delegate bool Thinger(string input, [MaybeNullWhen(false)] out IEnumerable<string>? errors);
        
//        public delegate bool Validator<TInput, TResult, TError>(TInput input, [MaybeNullWhen(false)] out TResult result, [MaybeNullWhen(true)] out IEnumerable<TError>? errors);

//        public static Validator<TInput, TResult, TError> Error<TInput, TResult, TError>(IEnumerable<TError> errors)
//        {
//            bool Validator(TInput input, out TResult result, out IEnumerable<TError> errors_)
//            {
//                result = default;
//                errors_ = errors;
//                return false;
//            }

//            return Validator;
//        }

//        public static Validator<TInput, TReturn, TError> SelectMany<TInput, TResult, TError, TReturn>(this Validator<TInput, TResult, TError> validator, Func<TResult, Validator<TInput, TReturn, TError>> bind)
//            x =>
//        {
//            Validator<TInput, TReturn, TError> CreateValidator(TInput input, out TResult result, out IEnumerable<TError> errors)
//            {
//                return validator(input, null, null) ? bind(result) : Error<TInput, TReturn, TError>(errors);
//            }

//            bool Validator(TInput input, out TResult result, out IEnumerable<TError> errors) =>
//                CreateValidator(input, out result, out errors)(input, out result, errors);

//            return Validator;
//        }
//        public static Thinger Combine(params Thinger[] thingers)
//        {
//            var d = default(Validator<string, string, string>);

//            bool CompositeThinger(string input, out IEnumerable<string> errors)
//            {
//                var result = false;
//                errors = Enumerable.Empty<string>();
//                foreach (var thinger in thingers)
//                {
//                    var result_ = thinger(input, out var errors_);
//                    if(!result_) errors.Concat(errors_);
//                    result = result && result_;
//                }
//                return result;
//            }

//            return CompositeThinger;
//        }

//        public static void DoThing()
//        {
//            var thingers = Combine(TryGet42);

//            var res = TryGet42("", out var errors_);

//            //thingers("", out var errors_);
//            AcceptValue(errors_);
//        }

//        public static void AcceptValue(IEnumerable<string> values) =>
//            throw new NotImplementedException();
//    }
//}
