using System.Threading.Tasks;

namespace Qx.Prelude
{
    public static class ValueTaskExtensions
    {
        public static ValueTask<T> ToValueTask<T>(this T @this) => new ValueTask<T>(@this);
    }
}
