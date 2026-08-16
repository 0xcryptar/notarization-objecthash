using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ObjectHashServer.BLL.Models.Api.Response
{
    public class ObjectHashResponseModel : ResponseModel
    {
        public ObjectHashResponseModel() : base("objectHash")
        {
            Algorithm = Globals.ALGORITHM_NAME;
        }

        public ObjectHashResponseModel(ObjectHash objectHash) : this()
        {
            Data = objectHash.Data;
            Salts = objectHash.Salts;
            Hash = objectHash.Hash;
            Algorithm = objectHash.Algorithm;
        }

        public JToken Data { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public JToken Salts { get; set; }
        public string Hash { get; set; }
        public string Algorithm { get; set; }
    }
}
