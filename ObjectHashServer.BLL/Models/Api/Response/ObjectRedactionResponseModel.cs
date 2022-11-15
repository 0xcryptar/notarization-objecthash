using Newtonsoft.Json.Linq;

namespace ObjectHashServer.BLL.Models.Api.Response
{
    public class ObjectRedactionResponseModel : ResponseModel
    {
        public ObjectRedactionResponseModel() : base("objectRedaction")
        { }

        public ObjectRedactionResponseModel(ObjectRedaction objectRedaction) : this()
        {
            Data = objectRedaction.RedactedData;
            Salts = objectRedaction.RedactedSalts;
            RedactSettings = objectRedaction.RedactSettings;
        }

        public JToken Data { get; set; }
        public JToken Salts { get; set; }
        public JToken RedactSettings { get; set; }
    }
}
