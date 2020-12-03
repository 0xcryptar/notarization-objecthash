using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ObjectHashServer.Exceptions;
using ObjectHashServer.Models.Api.Request;
using ObjectHashServer.Services.Implementations;

namespace ObjectHashServer.UnitTests.Services.Implementations
{
    [TestFixture]
    public class GenerateSaltsImplementationTests
    {
        [Test]
        public void SetRandomSaltsForObjectBaseRequestModel_WithSalts_WillThrowBadRequest()
        {
            ObjectBaseRequestModel request = new ObjectBaseRequestModel() { Data = new JObject(new JProperty("sample", "object")), Salts = new JObject(new JProperty("sample", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")) };
                        
            Assert.That(() => GenerateSaltsImplementation.SetRandomSaltsForObjectBaseRequestModel(request),
                Throws.Exception.TypeOf<BadRequestException>());
        }

        [Test]
        public void SetRandomSaltsForObjectBaseRequestModel_ForArray_WillSetSaltForEachElement()
        {
            ObjectBaseRequestModel request = new ObjectBaseRequestModel() { Data = new JArray("val1", "val2", "val3", "val4") };

            GenerateSaltsImplementation.SetRandomSaltsForObjectBaseRequestModel(request);

            foreach (JToken salt in request.Salts)
            {
                // assert that hex. hex requires 2 char for each byte
                Assert.That(((string)salt).Length, Is.EqualTo(Globals.HASH_ALGORITHM_BLOCK_SIZE * 2));
            }
        }

        [Test]
        public void SetRandomSaltsForObjectBaseRequestModel_ForObject_WillSetSaltForAllLeaves()
        {
            ObjectBaseRequestModel request = new ObjectBaseRequestModel() { Data = new JObject(new JProperty("obj1", new JObject(new JProperty("obj2", "val2")))) };

            GenerateSaltsImplementation.SetRandomSaltsForObjectBaseRequestModel(request);

            Assert.That(request.Salts["obj1"]["obj2"].Type, Is.EqualTo(JTokenType.String));
            Assert.That(((string)request.Salts["obj1"]["obj2"]).Length, Is.EqualTo(Globals.HASH_ALGORITHM_BLOCK_SIZE * 2));
        }

        [Test]
        public void SetRandomSaltsForObjectBaseRequestModel_ForRedactedValue_ReturnsNoSalt()
        {
            ObjectBaseRequestModel request = new ObjectBaseRequestModel() { Data = new JObject(new JProperty("obj1", new JObject(new JProperty("obj2", "**REDACTED**e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")))) };

            GenerateSaltsImplementation.SetRandomSaltsForObjectBaseRequestModel(request);

            Assert.That(request.Salts["obj1"]["obj2"].Type, Is.EqualTo(JTokenType.String));            
            Assert.That((string)request.Salts["obj1"]["obj2"], Is.EqualTo("**REDACTED**"));
        }
    }
}