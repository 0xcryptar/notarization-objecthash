using ObjectHashServer.BLL.Utils;
using Xunit;

namespace ObjectHashServer.UnitTests.Utils
{
    public class JcsNumberFormatterTests
    {
        [Theory]
        [InlineData(0.0, "0")]
        [InlineData(-0.0, "0")]
        [InlineData(100.0, "100")]
        [InlineData(-5.0, "-5")]
        [InlineData(3.14, "3.14")]
        [InlineData(-3.14, "-3.14")]
        [InlineData(1e21, "1e+21")]
        [InlineData(1e-7, "1e-7")]
        public void FormatNumber_ShouldMatchRfc8785Specification(double input, string expected)
        {
            string actual = JcsNumberFormatter.FormatNumber(input);
            Assert.Equal(expected, actual);
        }
    }
}
