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

        // returns calculated Hash
        public string Hash
        {
            get
            {
                // this is the real call to the object hash implementation
                ObjectHashImplementation h = new ObjectHashImplementation(Salt);
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

                JsonRedactionImplementation service = new JsonRedactionImplementation(Salt);
                return service.RedactJson(Data, RedactSettings);
            }
        }
    }
}
