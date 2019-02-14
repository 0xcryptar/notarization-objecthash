using System;
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

        // TODO: optimize
        private JToken redactedData;
        public JToken RedactedData
        {
            get { UpdateRedactedData(); return redactedData; }
            private set { redactedData = value; }
        }
        private JToken redactedSalts;
        public JToken RedactedSalts
        {
            get { UpdateRedactedData(); return redactedSalts; }
            private set { redactedSalts = value; }
        }

        private void UpdateRedactedData()
        {
            ObjectRedactionImplementation r = new ObjectRedactionImplementation();
            (RedactedData, RedactedSalts) = r.RedactJToken(Data, RedactSettings, Salts);
        }
    }
}
