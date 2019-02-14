using Newtonsoft.Json.Linq;
using ObjectHashServer.Models.API.Request;
using ObjectHashServer.Services.Implementations;

namespace ObjectHashServer.Models
{
    public class ObjectRedaction
    {
        public ObjectRedaction(ObjectRedactionRequestModel model)
        {
            Data = model.Data;
            RedactSettings = model.RedactSettings;
            Salts = model.Salts;
        }

        public JToken Data { get; set; }
        public JToken RedactSettings { get; set; }
        public JToken Salts { get; set; }

        public JToken RedactedData
        {
            get
            {
                if (RedactSettings == null)
                {
                    return Data;
                }

                // TODO: JsonRedactionImplementation service = new JsonRedactionImplementation();
                // return service.RedactJson(Data, RedactSettings, Salts);
                return null;
            }
        }
    }
}
