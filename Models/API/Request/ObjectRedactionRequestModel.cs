using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace ObjectHashServer.Models.Api.Request
{
    public class ObjectRedactionRequestModel
    {
        [Required]
        public JToken Data { get; set; }
        [Required]
        public JToken RedactSettings { get; set; }
        // optional
        public JToken Salts { get; set; }
    }
}

