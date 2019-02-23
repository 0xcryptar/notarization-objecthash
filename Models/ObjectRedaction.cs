using Newtonsoft.Json.Linq;
using ObjectHashServer.Models.Api.Request;
using ObjectHashServer.Services.Implementations;

namespace ObjectHashServer.Models
{
    public class ObjectRedaction
    {
        public ObjectRedaction(ObjectRedactionRequestModel model)
        {
            Data = model.Data;
            Salts = model.Salts;
            RedactSettings = model.RedactSettings;
        }

        public JToken Data { get; set; }
        public JToken Salts { get; set; }
        public JToken RedactSettings { get; set; }

        public JToken RedactedData
        {
            get
            {
                ObjectRedactionImplementation r = new ObjectRedactionImplementation();
                (JToken redactedData, _) = r.RedactJToken(Data, RedactSettings, Salts);
                return redactedData;
            }
        }

        public JToken RedactedSalts
        {
            get
            {
                ObjectRedactionImplementation r = new ObjectRedactionImplementation();
                (_, JToken redactedSalts) = r.RedactJToken(Data, RedactSettings, Salts);
                return redactedSalts;
            }
        }
    }
}
