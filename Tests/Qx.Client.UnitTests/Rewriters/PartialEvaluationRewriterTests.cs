using Qx.Client.Rewriters;
using System.Linq.Expressions;
using Xunit;

namespace Qx.Client.UnitTests.Rewriters
{
    public class PartialEvaluationRewriterTests
    {
        [Fact]
        public void Should_evaluate_simple_expression()
        {
            var expression = Expression.Add(Expression.Constant(2), Expression.Constant(40));

            var result = PartialEvaluationRewriter.Rewrite(expression);

            Assert.Equal(ExpressionType.Constant, result.NodeType);
            Assert.Equal(42, ((ConstantExpression)result).Value);
        }

        [Fact]
        public void Should_leave_parameter_expression()
        {
            var expression = Expression.Add(Expression.Add(Expression.Constant(2), Expression.Constant(40)), Expression.Parameter(typeof(int), "X"));

            var result = PartialEvaluationRewriter.Rewrite(expression);

            Assert.Equal(ExpressionType.Add, result.NodeType);
            Assert.Equal(ExpressionType.Parameter, ((BinaryExpression)result).Right.NodeType);
            Assert.Equal("X", ((ParameterExpression)((BinaryExpression)result).Right).Name);
        }
    }
}
