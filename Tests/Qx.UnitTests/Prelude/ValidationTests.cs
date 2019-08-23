using Qx.Prelude;
using System.Linq;
using Xunit;

namespace Qx.UnitTests.Prelude
{
    public class ValidationTests
    {
        private static Validation<string, int> IsEven(int i) =>
            i % 2 == 0 ? new Validation<string, int>(i) : new Validation<string, int>($"{i} is not an even number");

        [Fact] // TODO: turn this into a property test 'coz there's laws for this
        public void Traverse_should_traverse()
        {
            var xs = new[] { 1, 2, 3, 4 };
            var results = xs.AsEnumerable().TraverseA(IsEven);

            _ = results.Match(
                Valid: v =>
                {
                    Assert.True(false);
                    return Unit.Default;
                },
                Invalid: e =>
                {
                    Assert.NotEmpty(e);
                    Assert.Collection(e, e => e.StartsWith("1"), e => e.StartsWith("3"));
                    return Unit.Default;
                });
        }
    }
}
