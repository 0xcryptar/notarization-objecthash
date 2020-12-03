using Newtonsoft.Json.Linq;

namespace ObjectHashServer.Models.Api.Response
{
    public class ObjectHashResponseModel : ResponseModel
    {
        public ObjectHashResponseModel() : base("objectHash")
        { }

        public ObjectHashResponseModel(ObjectHash objectHash) : this()
        {
            Data = objectHash.Data;
            Salts = objectHash.Salts;
            Hash = objectHash.Hash;
        }

        public JToken Data { get; set; }
        public JToken Salts { get; set; }
        public string Hash { get; set; }
    }
}
