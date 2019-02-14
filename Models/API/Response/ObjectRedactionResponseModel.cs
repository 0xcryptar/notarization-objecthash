using Newtonsoft.Json.Linq;

namespace ObjectHashServer.Models.API.Response
{
    public class ObjectRedactionResponseModel : ResponseModel
    {
        public ObjectRedactionResponseModel() : base("objectRedaction")
        { }

        public ObjectRedactionResponseModel(ObjectRedaction objectRedaction) : this()
        {
            // TODO: return redacted data instead of data or as now property?
            Data = objectRedaction.RedactedData;
            RedactSettings = objectRedaction.RedactSettings;
            Salts = objectRedaction.Salts;
        }

        public JToken Data { get; set; }
        public JToken RedactSettings { get; set; }
        public JToken Salts { get; set; }
    }
}
