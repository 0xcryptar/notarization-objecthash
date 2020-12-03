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

        private JToken Data { get; }
        private JToken Salts { get; }
        public JToken RedactSettings { get; }

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
