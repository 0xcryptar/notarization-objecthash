using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Services.Implementations;

namespace ObjectHashServer.BLL.Models
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
