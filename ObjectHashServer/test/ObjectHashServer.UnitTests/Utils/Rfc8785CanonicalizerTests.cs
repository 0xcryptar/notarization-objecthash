using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ObjectHashServer.BLL;
using ObjectHashServer.BLL.Models;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Utils;

namespace ObjectHashServer.UnitTests.Utils
{
    [TestFixture]
    public class Rfc8785CanonicalizerTests
    {
        [Test]
        public void Canonicalize_OfficialRfc8785Section322Example_ShouldMatchOfficialOutput()
        {
            string rawJson = @"{
                ""numbers"": [333333333.33333329, 1E30, 4.50, 2e-3, 0.000000000000000000000000001],
                ""string"": ""\u20ac$\u000F\u000aA'\u0042\u0022\u005c\\\"\"/"",
                ""literals"": [null, true, false]
            }";

            JToken token = JToken.Parse(rawJson);
            string canonical = Rfc8785Canonicalizer.Canonicalize(token);

            string expectedCanonical = "{\"literals\":[null,true,false],\"numbers\":[333333333.3333333,1e+30,4.5,0.002,1e-27],\"string\":\"€$\\u000f\\nA'B\\\"\\\\\\\\\\\"/\"}";

            Assert.That(canonical, Is.EqualTo(expectedCanonical));
        }

        [Test]
        public void Canonicalize_OfficialRfc8785PropertySortingTest_ShouldSortKeysByUtf16CodeUnits()
        {
            string rawJson = @"{
                ""\u20ac"": ""Euro Sign"",
                ""\r"": ""Carriage Return"",
                ""\ufb33"": ""Hebrew Letter Dalet With Dagesh"",
                ""1"": ""One"",
                ""\ud83d\ude00"": ""Emoji: Grinning Face"",
                ""\u0080"": ""Control"",
                ""\u00f6"": ""Latin Small Letter O With Diaeresis""
            }";

            JToken token = JToken.Parse(rawJson);
            string canonical = Rfc8785Canonicalizer.Canonicalize(token);

            JObject parsedCanonical = JObject.Parse(canonical);
            string[] keysInOrder = parsedCanonical.Properties().Select(p => p.Name).ToArray();

            string[] expectedOrder = new string[]
            {
                "\r",
                "1",
                "\u0080",
                "\u00f6",
                "\u20ac",
                "\ud83d\ude00",
                "\ufb33"
            };

            Assert.That(keysInOrder, Is.EqualTo(expectedOrder));
        }

        [Test]
        public void ComputeHash_OfficialRfc8785Bytes_ShouldMatchSha256Digest()
        {
            string rawJson = @"{
                ""numbers"": [333333333.33333329, 1E30, 4.50, 2e-3, 0.000000000000000000000000001],
                ""string"": ""\u20ac$\u000F\u000aA'\u0042\u0022\u005c\\\"\"/"",
                ""literals"": [null, true, false]
            }";

            JToken token = JToken.Parse(rawJson);
            byte[] hashBytes = Rfc8785Canonicalizer.ComputeHash(token);
            string hashHex = HexConverter.ToHex(hashBytes);

            string canonicalText = "{\"literals\":[null,true,false],\"numbers\":[333333333.3333333,1e+30,4.5,0.002,1e-27],\"string\":\"€$\\u000f\\nA'B\\\"\\\\\\\\\\\"/\"}";
            byte[] expectedBytes = Encoding.UTF8.GetBytes(canonicalText);
            using SHA256 sha = SHA256.Create();
            string expectedHex = HexConverter.ToHex(sha.ComputeHash(expectedBytes));

            Assert.That(hashHex, Is.EqualTo(expectedHex));
        }

        [Test]
        public void ObjectHash_AlgorithmSwitching_ShouldProduceDifferentHashesForCryptarAndRfc8785()
        {
            var json = JObject.Parse("{\"b\": 2, \"a\": 1}");

            var cryptarRequest = new ObjectBaseRequestModel
            {
                Data = json,
                Algorithm = Globals.ALGORITHM_CRYPTAR_V1_SHA256
            };
            var cryptarObjectHash = new ObjectHash(cryptarRequest);

            var rfc8785Request = new ObjectBaseRequestModel
            {
                Data = json,
                Algorithm = Globals.ALGORITHM_RFC8785_V1_SHA256
            };
            var rfc8785ObjectHash = new ObjectHash(rfc8785Request);

            Assert.That(cryptarObjectHash.Algorithm, Is.EqualTo("cryptar-v1-sha256"));
            Assert.That(rfc8785ObjectHash.Algorithm, Is.EqualTo("rfc8785-v1-sha256"));

            Assert.That(cryptarObjectHash.Hash, Is.Not.EqualTo(rfc8785ObjectHash.Hash));
        }
    }
}
