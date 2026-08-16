using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL;
using ObjectHashServer.BLL.Models;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Services.Implementations;
using Xunit;

namespace ObjectHashServer.UnitTests.Services.Implementations
{
    public class ObjectHashImplementationTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void HashJToken_ShouldProduceDeterministicHashForCryptarV1Sha256()
        {
            var json1 = JObject.Parse("{\"b\": 2, \"a\": 1, \"pi\": 3.14}");
            var json2 = JObject.Parse("{\"pi\": 3.14, \"a\": 1, \"b\": 2}");

            var hasher1 = new ObjectHashImplementation();
            hasher1.HashJToken(json1);
            string hash1 = hasher1.HashAsString();

            var hasher2 = new ObjectHashImplementation();
            hasher2.HashJToken(json2);
            string hash2 = hasher2.HashAsString();

            Assert.NotNull(hash1);
            Assert.Equal(64, hash1.Length);
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ObjectHash_ShouldSupportSha512AndBlake3ForCryptarAndRfc8785()
        {
            var json = JObject.Parse("{\"test\": \"hello\", \"value\": 42}");

            var sha512Cryptar = new ObjectHash(new ObjectBaseRequestModel { Data = json, Algorithm = "cryptar-v1-sha512" });
            Assert.Equal(128, sha512Cryptar.Hash.Length);

            var blake3Cryptar = new ObjectHash(new ObjectBaseRequestModel { Data = json, Algorithm = "cryptar-v1-blake3" });
            Assert.Equal(64, blake3Cryptar.Hash.Length);

            var sha512Rfc = new ObjectHash(new ObjectBaseRequestModel { Data = json, Algorithm = "rfc8785-v1-sha512" });
            Assert.Equal(128, sha512Rfc.Hash.Length);

            var blake3Rfc = new ObjectHash(new ObjectBaseRequestModel { Data = json, Algorithm = "rfc8785-v1-blake3" });
            Assert.Equal(64, blake3Rfc.Hash.Length);

            Assert.NotEqual(sha512Cryptar.Hash, sha512Rfc.Hash);
            Assert.NotEqual(blake3Cryptar.Hash, blake3Rfc.Hash);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GenerateSalts_ShouldSupport128BitAnd256BitSecureSalts()
        {
            var json = JObject.Parse("{\"user\": \"Alice\", \"age\": 30}");

            JToken salts128 = GenerateSaltsImplementation.SaltsForJToken(json, 128);
            string salt128Val = (string)salts128["user"];
            Assert.Equal(32, salt128Val.Length); // 16 bytes = 32 hex chars

            JToken salts256 = GenerateSaltsImplementation.SaltsForJToken(json, 256);
            string salt256Val = (string)salts256["user"];
            Assert.Equal(64, salt256Val.Length); // 32 bytes = 64 hex chars

            // Verify hashing works seamlessly with 128-bit salts
            var oh128 = new ObjectHash(new ObjectBaseRequestModel { Data = json, Salts = salts128, Algorithm = "cryptar-v1-sha256" });
            Assert.Equal(64, oh128.Hash.Length);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void HashJToken_ShouldSupportRedactionAndSalts()
        {
            var json = JObject.Parse("{\"secret\": \"mySecret\", \"public\": \"hello\"}");
            var salts = JObject.Parse("{\"secret\": \"000102030405060708090a0b0c0d0e0f000102030405060708090a0b0c0d0e0f\", \"public\": \"000102030405060708090a0b0c0d0e0f000102030405060708090a0b0c0d0e0f\"}");
            var redactSettings = JObject.Parse("{\"secret\": true}");

            (JToken redactedJson, JToken redactedSalts) = ObjectRedactionImplementation.RedactJToken(json, redactSettings, salts);

            var originalHash = new ObjectHash(new ObjectBaseRequestModel { Data = json, Salts = salts }).Hash;
            var redactedHash = new ObjectHash(new ObjectBaseRequestModel { Data = redactedJson, Salts = redactedSalts }).Hash;

            Assert.Equal(originalHash, redactedHash);
        }
    }
}