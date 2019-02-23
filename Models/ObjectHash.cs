using Newtonsoft.Json.Linq;
using ObjectHashServer.Models.Api.Request;
using ObjectHashServer.Services.Implementations;

namespace ObjectHashServer.Models
{
    public class ObjectHash
    {
        public ObjectHash(ObjectHashRequestModel model)
        {
            Data = model.Data;
            Salts = model.Salts;
        }

        public JToken Data { get; set; }
        public JToken Salts { get; set; }

        public string Hash
        {
            get
            {
                ObjectHashImplementation h = new ObjectHashImplementation();
                h.HashJToken(Data, Salts);
                return h.HashAsString();
            }
        }
    }
}
