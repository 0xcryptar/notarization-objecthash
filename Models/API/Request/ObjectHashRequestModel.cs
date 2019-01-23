using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace ObjectHashServer.Models.API.Request
{
    public class ObjectHashRequestModel
    {
        [Required]
        public JToken Data { get; set; }
        // optional
        public JToken RedactSettings { get; set; }
        // optional
        public string Salt { get; set; }
    }
}
