using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ObjectHashServer.BLL;
using ObjectHashServer.BLL.Models;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Services.Implementations;

namespace ObjectHashServer.UnitTests.Services.Implementations
{
    [TestFixture]
    public class ObjectHashImplementationTests
    {
        [Test]
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

            Assert.That(hash1, Is.Not.Null);
            Assert.That(hash1.Length, Is.EqualTo(64));
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void ObjectHash_ShouldSupportSha512AndBlake3ForCryptarAndRfc8785()
        {
            var json = JObject.Parse("{\"test\": \"hello\", \"value\": 42}");

            var sha512Cryptar = new ObjectHash(new ObjectBaseRequestModel { Data = json, Algorithm = "cryptar-v1-sha512" });
            Assert.That(sha512Cryptar.Hash.Length, Is.EqualTo(128));

            var blake3Cryptar = new ObjectHash(new ObjectBaseRequestModel { Data = json, Algorithm = "cryptar-v1-blake3" });
            Assert.That(blake3Cryptar.Hash.Length, Is.EqualTo(64));

            var sha512Rfc = new ObjectHash(new ObjectBaseRequestModel { Data = json, Algorithm = "rfc8785-v1-sha512" });
            Assert.That(sha512Rfc.Hash.Length, Is.EqualTo(128));

            var blake3Rfc = new ObjectHash(new ObjectBaseRequestModel { Data = json, Algorithm = "rfc8785-v1-blake3" });
            Assert.That(blake3Rfc.Hash.Length, Is.EqualTo(64));

            Assert.That(sha512Cryptar.Hash, Is.Not.EqualTo(sha512Rfc.Hash));
            Assert.That(blake3Cryptar.Hash, Is.Not.EqualTo(blake3Rfc.Hash));
        }

        [Test]
        public void ObjectHash_Model_ShouldSetDefaultAlgorithmToCryptarV1Sha256()
        {
            var request = new ObjectBaseRequestModel
            {
                Data = JObject.Parse("{\"test\": 123}")
            };

            var oh = new ObjectHash(request);

            Assert.That(oh.Algorithm, Is.EqualTo(Globals.ALGORITHM_NAME));
            Assert.That(oh.Algorithm, Is.EqualTo("cryptar-v1-sha256"));
            Assert.That(string.IsNullOrEmpty(oh.Hash), Is.False);
        }

        [Test]
        public void GenerateSalts_ShouldSupport128BitAnd256BitSecureSalts()
        {
            var json = JObject.Parse("{\"user\": \"Alice\", \"age\": 30}");

            JToken salts128 = GenerateSaltsImplementation.SaltsForJToken(json, 128);
            string salt128Val = (string)salts128["user"];
            Assert.That(salt128Val.Length, Is.EqualTo(32));

            JToken salts256 = GenerateSaltsImplementation.SaltsForJToken(json, 256);
            string salt256Val = (string)salts256["user"];
            Assert.That(salt256Val.Length, Is.EqualTo(64));

            var oh128 = new ObjectHash(new ObjectBaseRequestModel { Data = json, Salts = salts128, Algorithm = "cryptar-v1-sha256" });
            Assert.That(oh128.Hash.Length, Is.EqualTo(64));
        }

        [Test]
        public void HashJToken_ShouldSupportRedactionAndSalts()
        {
            var json = JObject.Parse("{\"secret\": \"mySecret\", \"public\": \"hello\"}");
            var salts = JObject.Parse("{\"secret\": \"000102030405060708090a0b0c0d0e0f000102030405060708090a0b0c0d0e0f\", \"public\": \"000102030405060708090a0b0c0d0e0f000102030405060708090a0b0c0d0e0f\"}");
            var redactSettings = JObject.Parse("{\"secret\": true}");

            (JToken redactedJson, JToken redactedSalts) = ObjectRedactionImplementation.RedactJToken(json, redactSettings, salts);

            var originalHash = new ObjectHash(new ObjectBaseRequestModel { Data = json, Salts = salts }).Hash;
            var redactedHash = new ObjectHash(new ObjectBaseRequestModel { Data = redactedJson, Salts = redactedSalts }).Hash;

            Assert.That(originalHash, Is.EqualTo(redactedHash));
        }
    }
}