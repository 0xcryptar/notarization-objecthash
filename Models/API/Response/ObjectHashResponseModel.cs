using Newtonsoft.Json.Linq;

namespace ObjectHashServer.Models.API.Response
{
    public class ObjectHashResponseModel : ResponseModel
    {
        public ObjectHashResponseModel() : base("objectHash")
        { }

        public ObjectHashResponseModel(ObjectHash objectHash) : this()
        {
            // only return the redacted data
            Data = objectHash.RedactedData;
            Hash = objectHash.Hash;
            RedactSettings = objectHash.RedactSettings;
            Salt = objectHash.Salt;
        }

        public JToken Data { get; set; }
        public string Hash { get; set; }
        public JToken RedactSettings { get; set; }
        public string Salt { get; set; }
    }
}
