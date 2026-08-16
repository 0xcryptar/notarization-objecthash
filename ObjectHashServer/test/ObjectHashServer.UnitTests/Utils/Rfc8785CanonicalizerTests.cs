using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL;
using ObjectHashServer.BLL.Models;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Utils;
using Xunit;

namespace ObjectHashServer.UnitTests.Utils
{
    public class Rfc8785CanonicalizerTests
    {
        [Fact]
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

            Assert.Equal(expectedCanonical, canonical);
        }

        [Fact]
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

            Assert.Equal(expectedOrder, keysInOrder);
        }

        [Fact]
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

            // Compute reference SHA-256 over exact RFC canonical UTF-8 bytes
            string canonicalText = "{\"literals\":[null,true,false],\"numbers\":[333333333.3333333,1e+30,4.5,0.002,1e-27],\"string\":\"€$\\u000f\\nA'B\\\"\\\\\\\\\\\"/\"}";
            byte[] expectedBytes = Encoding.UTF8.GetBytes(canonicalText);
            using SHA256 sha = SHA256.Create();
            string expectedHex = HexConverter.ToHex(sha.ComputeHash(expectedBytes));

            Assert.Equal(expectedHex, hashHex);
        }

        [Fact]
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

            Assert.Equal("cryptar-v1-sha256", cryptarObjectHash.Algorithm);
            Assert.Equal("rfc8785-v1-sha256", rfc8785ObjectHash.Algorithm);

            // Hashes must differ because cryptar uses Merkle tree tagged hashing, rfc8785 uses JCS text SHA256
            Assert.NotEqual(cryptarObjectHash.Hash, rfc8785ObjectHash.Hash);
        }
    }
}
