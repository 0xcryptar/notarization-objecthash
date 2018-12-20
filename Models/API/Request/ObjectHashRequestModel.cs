    using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace ObjectHashServer.Models.API.Request
{
    public class ObjectHashRequestModel
    {
        [Required]
        public JObject Data { get; set; }
        // optional
        public JObject RedactSettings { get; set; }
        // optional
        public string Salt { get; set; }
    }
}
