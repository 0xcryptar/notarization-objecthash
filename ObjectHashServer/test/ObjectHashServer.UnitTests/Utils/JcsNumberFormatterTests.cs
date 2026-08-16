using NUnit.Framework;
using ObjectHashServer.BLL.Utils;

namespace ObjectHashServer.UnitTests.Utils
{
    [TestFixture]
    public class JcsNumberFormatterTests
    {
        [TestCase(0.0, "0")]
        [TestCase(-0.0, "0")]
        [TestCase(100.0, "100")]
        [TestCase(-5.0, "-5")]
        [TestCase(3.14, "3.14")]
        [TestCase(-3.14, "-3.14")]
        [TestCase(1e21, "1e+21")]
        [TestCase(1e-7, "1e-7")]
        public void FormatNumber_ShouldMatchRfc8785Specification(double input, string expected)
        {
            string actual = JcsNumberFormatter.FormatNumber(input);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
