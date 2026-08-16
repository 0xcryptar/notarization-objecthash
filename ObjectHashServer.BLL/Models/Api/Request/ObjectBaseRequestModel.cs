using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace ObjectHashServer.BLL.Models.Api.Request
{
    [OpenApiExample(typeof(ObjectBaseRequestModelExample))]
    public class ObjectBaseRequestModel
    {
        [Required]
        [JsonRequired]
        public JToken Data { get; set; }
        // optional
        public JToken Salts { get; set; }
        // optional algorithm identifier, defaults to cryptar-v1-sha256
        public string Algorithm { get; set; } = Globals.ALGORITHM_NAME;
        // optional salt length in bits (128 or 256, default 256)
        public int SaltBitLength { get; set; } = Globals.DEFAULT_SALT_BIT_LENGTH;
    }
}
