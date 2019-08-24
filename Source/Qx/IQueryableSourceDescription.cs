using System.Reflection;

namespace Qx
{
    /// <summary>
    /// A runtime source for a query
    /// </summary>
    public interface IQueryableSourceDescription
    {
        MethodInfo Method { get; }
        object Instance { get; }
    }
}
