using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using ObjectHashServer.BLL.Models.Api.Request;

namespace ObjectHashServer.BLL.Models
{
    public class ObjectBaseRequestModelExample : OpenApiExample<ObjectBaseRequestModel>
    {
        public override IOpenApiExample<ObjectBaseRequestModel> Build(NamingStrategy namingStrategy = null)
        {
            JToken data = JToken.Parse("{\r\n  \"user\": \"Alice\",\r\n  \"role\": \"Admin\",\r\n  \"status\": \"Active\"\r\n}");
            JToken salts = JToken.Parse("{\r\n  \"user\": \"a72d1eae4b784712756abc2d9ecfc58bb2218cb6f5e1d85bc13b4bd540222a73\",\r\n  \"role\": \"1881377ccc3422f892c463b90e0d74cbd3b04877fba6866d95875960dccf5826\",\r\n  \"status\": \"4c94a79ee1c0c984844cf6cbbf08cb51b5588e14674a9925441bb7200b9ecdf6\"\r\n}");

            this.Examples.Add(OpenApiExampleResolver.Resolve("ObjectBaseRequestModelExample", new ObjectBaseRequestModel()
            {
                Data = data,
                Salts = salts,
                Algorithm = "cryptar-v1-sha256",
                SaltBitLength = 256
            }, namingStrategy));

            return this;
        }
    }
}
