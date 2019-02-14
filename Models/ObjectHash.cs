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

        // TODO: optimize
        private string hash;
        public string Hash
        {
            get { UpdateHash(); return hash; }
            private set { hash = value; }
        }

        private void UpdateHash()
        {
            // calculate hash
            ObjectHashImplementation h = new ObjectHashImplementation();
            h.HashJToken(Data, Salts);
            Hash = h.HashAsString();
        }
    }
}
