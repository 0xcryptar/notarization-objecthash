using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ObjectHashServer.Controllers;
using ObjectHashServer.BLL.Models.Api.Request;
namespace ObjectHashServer.UnitTests.Controllers
{
    [TestFixture]
    public class ObjectHashControllerTests
    {
        [Test]
        public void Post_WithoutSalts_WillGenerateNewSalts()
        {
            ObjectHashController controller = new ObjectHashController();
            ObjectHashRequestModel request = new ObjectHashRequestModel() { Data = new JObject(new JProperty("sample", "object")), Salts = null };
            
            var result = controller.Post(request, true);
            
            Assert.That(result.Value.Salts, Is.Not.Null);
        }
        
        [Test]
        public void Post_WithSalts_WillNotGenerateNewSalts()
        {
            ObjectHashController controller = new ObjectHashController();
            ObjectHashRequestModel request = new ObjectHashRequestModel() { Data = new JObject(new JProperty("sample", "object")), Salts = new JObject(new JProperty("sample", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")) };
            
            var result = controller.Post(request, false);
            
            Assert.That(result.Value.Salts, Is.EqualTo(new JObject(new JProperty("sample", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"))));
        }
    }
}