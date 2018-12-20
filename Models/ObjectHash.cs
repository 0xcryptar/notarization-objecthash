using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;
using ObjectHashServer.Models.API.Request;
using ObjectHashServer.Services.Implementations;

namespace ObjectHashServer.Models
{
    public class ObjectHash
    {
        public ObjectHash(ObjectHashRequestModel model)
        {
            Data = model.Data;
            RedactSettings = model.RedactSettings;
            Salt = model.Salt;
        }

        public JObject Data { get; set; }
        public JObject RedactSettings { get; set; }
        public string Salt { get; set; }

        // returns calculated Hash
        public string Hash
        {
            get
            {
                // this is the real call to the object hash implementation
                ObjectHashImplementation h = new ObjectHashImplementation();
                h.HashAny(Data);
                return h.ToHex(); 
            }
        }

        // returns Data JObject where fields are redacted with the ShareSettings
        public JObject RedactedData
        {
            get
            {
                if (RedactSettings == null)
                {
                    return Data;
                }

                JsonRedactionImplementation service = new JsonRedactionImplementation();
                return service.RedactJson(Data, RedactSettings);
            }
        }
    }
}
