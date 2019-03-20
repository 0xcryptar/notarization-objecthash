using Newtonsoft.Json.Linq;
using ObjectHashServer.Models.Api.Request;
using ObjectHashServer.Services.Implementations;

namespace ObjectHashServer.Models
{
    public class ObjectHash
    {
        public ObjectHash(ObjectBaseRequestModel model)
        {
            Data = model.Data;
            Salts = model.Salts;
        }

        public JToken Data { get; }
        public JToken Salts { get; }

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
