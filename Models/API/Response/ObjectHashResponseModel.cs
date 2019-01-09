using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace ObjectHashServer.Models.API.Response
{
    public class ObjectHashResponseModel
    {
        public ObjectHashResponseModel(ObjectHash objectHash)
        {
            // only return the redacted data
            Data = objectHash.RedactedData;
            Hash = objectHash.Hash;
            RedactSettings = objectHash.RedactSettings;
            Salt = objectHash.Salt;
        }

        // required
        public JToken Data { get; set; }
        // required
        public string Hash { get; set; }
        // optional
        public JToken RedactSettings { get; set; }
        // optional
        public string Salt { get; set; }
    }
}
