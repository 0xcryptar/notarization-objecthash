using Newtonsoft.Json.Linq;
using ObjectHashServer.Models.API.Request;
using ObjectHashServer.Services.Implementations;

namespace ObjectHashServer.Models
{
    public class ObjectHash
    {
        public ObjectHash(ObjectHashRequestModel model)
        {
            Data = model.Data;
            RedactSettings = model.RedactSettings;
            Salt = model.Salt;
        }

        public JToken Data { get; set; }
        public JToken RedactSettings { get; set; }
        public string Salt { get; set; }

        public string Hash
        {
            get
            {
                // do not add the salt here as we want the hash calculation to
                // be independent of the salt
                ObjectHashImplementation h = new ObjectHashImplementation();
                h.HashJToken(Data);
                return h.HashAsString(); 
            }
        }

        // returns Data JObject where fields are redacted with the ShareSettings
        public JToken RedactedData
        {
            get
            {
                if (RedactSettings == null)
                {
                    return Data;
                }

                // the redaction should be salt depending 
                JsonRedactionImplementation service = new JsonRedactionImplementation(Salt);
                return service.RedactJson(Data, RedactSettings);
            }
        }
    }
}
