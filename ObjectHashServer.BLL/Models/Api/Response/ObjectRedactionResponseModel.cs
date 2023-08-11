using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Services.Implementations;

namespace ObjectHashServer.BLL.Models.Api.Response
{
    public class ObjectRedactionResponseModel : ResponseModel
    {
        public ObjectRedactionResponseModel() : base("objectRedaction")
        { }

        public ObjectRedactionResponseModel(ObjectRedaction objectRedaction) : this()
        {
            var oh = new ObjectHash(new ObjectBaseRequestModel() { Data = objectRedaction.RedactedData, Salts = objectRedaction.RedactedSalts });
            Data = oh.Data;
            Salts = oh.Salts;
            Hash = oh.Hash;
        }

        public JToken Data { get; set; }
        public JToken Salts { get; set; }
        public string Hash { get; set; }
    }
}
