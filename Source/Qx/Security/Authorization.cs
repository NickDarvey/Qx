using Qx.Prelude;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Qx.Security
{
    public delegate ValueTask<Validation<string, Unit>> Authorizer<TMethodDescription>(IEnumerable<TMethodDescription> bindings) where TMethodDescription : IQueryableSourceDescription;

    public static class Authorization
    {
        /// <summary>
        /// A validation that represents an authorization of method bindings.
        /// </summary>
        public static readonly Validation<string, Unit> Authorized = new Validation<string, Unit>(Unit.Default);

        /// <summary>
        /// A validation that represents an empty forbidding of method bindings.
        /// </summary>
        public static readonly Validation<string, Unit> Forbidden = new Validation<string, Unit>();

        /// <summary>
        /// A validation that represents an authorization of method bindings.
        /// </summary>
        public static readonly ValueTask<Validation<string, Unit>> AuthorizedTask = new ValueTask<Validation<string, Unit>>(Authorized);

        /// <summary>
        /// A validation that represents an empty forbidding of method bindings.
        /// </summary>
        public static readonly ValueTask<Validation<string, Unit>> ForbiddenTask = new ValueTask<Validation<string, Unit>>(Forbidden);

        public static Validation<string, Unit> Forbid(IEnumerable<string> reasons) => new Validation<string, Unit>(reasons);

    }
}
