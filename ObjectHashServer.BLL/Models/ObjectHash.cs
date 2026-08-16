using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Services.Implementations;

using ObjectHashServer.BLL.Utils;

namespace ObjectHashServer.BLL.Models
{
    public class ObjectHash
    {
        public ObjectHash(ObjectBaseRequestModel model)
        {
            Data = model.Data;
            Salts = model.Salts;
            Algorithm = string.IsNullOrEmpty(model.Algorithm) ? Globals.ALGORITHM_CRYPTAR_V1_SHA256 : model.Algorithm;
        }

        public JToken Data { get; }
        public JToken Salts { get; }
        public string Algorithm { get; }

        public string Hash
        {
            get
            {
                HashAlgorithmType algorithmType = HashHelper.ParseHashAlgorithmType(Algorithm);

                if (Algorithm.StartsWith("rfc8785", System.StringComparison.OrdinalIgnoreCase))
                {
                    byte[] hashBytes = Rfc8785Canonicalizer.ComputeHash(Data, algorithmType);
                    return HexConverter.ToHex(hashBytes);
                }
                else
                {
                    ObjectHashImplementation h = new ObjectHashImplementation(algorithmType);
                    h.HashJToken(Data, Salts);
                    return h.HashAsString();
                }
            }
        }
    }
}
