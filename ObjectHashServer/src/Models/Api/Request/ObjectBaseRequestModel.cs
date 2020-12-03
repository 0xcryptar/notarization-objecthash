using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace ObjectHashServer.Models.Api.Request
{
    public class ObjectBaseRequestModel
    {
        [Required]
        public JToken Data { get; set; }
        // optional
        public JToken Salts { get; set; }
    }
}
