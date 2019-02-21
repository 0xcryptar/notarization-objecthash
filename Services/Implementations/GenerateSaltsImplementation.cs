using System;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;
using ObjectHashServer.Models.Api.Request;
using ObjectHashServer.Models.Extensions;
using ObjectHashServer.Utils;

namespace ObjectHashServer.Services.Implementations
{
    public class GenerateSaltsImplementation
    {
        public static void SetRandomSaltsForObjectBaseRequestModel(ObjectBaseRequestModel model)
        {
            if (!model.Salts.IsNullOrEmpty())
            {
                throw new BadRequestException("You want to generate new salts but you send salts with the request. Please either generate new salts or send them with the request.");
            }

            GenerateSaltsImplementation gsi = new GenerateSaltsImplementation();
            model.Salts = gsi.SaltsForJToken(model.Data);
        }

        public JToken SaltsForJToken(JToken json)
        {
            JToken jsonClone = json.DeepClone();
            return RecursivlyOverrideJTokenWithSalts(jsonClone);
        }

        private JToken RecursivlyOverrideJTokenWithSalts(JToken json)
        {
            switch (json.Type)
            {
                case JTokenType.Array:
                    {
                        return OverrideArrayWithSalts((JArray)json);
                    }
                case JTokenType.Object:
                    {
                        return OverrideObjectWithSalts((JObject)json);
                    }
                case JTokenType.String:
                    {
                        if(((string)json).StartsWith("**REDACTED**", Globals.STRING_COMPARE_METHOD))
                        {
                            return "**REDACTED*";
                        }

                        return GenerateSaltForLeaf();
                    }
                case JTokenType.Integer:
                case JTokenType.TimeSpan:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.Null:
                case JTokenType.None:
                case JTokenType.Boolean:
                case JTokenType.Float:
                case JTokenType.Bytes:
                case JTokenType.Date:
                    {
                        return GenerateSaltForLeaf();
                    }
                default:
                    {
                        throw new BadRequestException($"The provided JSON has an invalid type of {json.Type}. Please remove it.");
                    }
            }
        }

        private string GenerateSaltForLeaf()
        {
            Random random = new Random();
            byte[] buffer = new byte[Globals.HASH_ALGORITHM_BLOCK_SIZE];
            random.NextBytes(buffer);
            return HexConverter.ToHex(buffer);
        }

        private JArray OverrideArrayWithSalts(JArray array)
        {
            JArray result = new JArray();
            for (int i = 0; i < array.Count; i++)
            {
                GenerateSaltsImplementation element = new GenerateSaltsImplementation();
                result.Add(element.RecursivlyOverrideJTokenWithSalts(array[i]));
            }
            return result;
        }

        private JObject OverrideObjectWithSalts(JObject obj)
        {
            JObject result = new JObject();
            foreach (var o in obj)
            {
                GenerateSaltsImplementation value = new GenerateSaltsImplementation();
                result[o.Key] = value.RecursivlyOverrideJTokenWithSalts(o.Value);
            }
            return result;
        }
    }
}
