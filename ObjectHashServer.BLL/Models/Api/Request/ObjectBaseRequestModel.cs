﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace ObjectHashServer.BLL.Models.Api.Request
{
    public class ObjectBaseRequestModel
    {
        [Required]
        [JsonRequired]
        public JToken Data { get; set; }
        // optional
        public JToken Salts { get; set; }
    }
}
