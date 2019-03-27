using System;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;
using ObjectHashServer.Models.Api.Request;
using ObjectHashServer.Models.Extensions;
using ObjectHashServer.Utils;

namespace ObjectHashServer.Services.Implementations
{
    public static class GenerateSaltsImplementation
    {
        public static void SetRandomSaltsForObjectBaseRequestModel(ObjectBaseRequestModel model)
        {
            if (!model.Salts.IsNullOrEmpty())
            {
                throw new BadRequestException("You want to generate new salts but you send salts with the request. Please either generate new salts or send them with the request.");
            }

            model.Salts = SaltsForJToken(model.Data);
        }

        private static JToken SaltsForJToken(JToken json)
        {
            JToken jsonClone = json.DeepClone();
            return RecursivelyOverrideJTokenWithSalts(jsonClone);
        }

        private static JToken RecursivelyOverrideJTokenWithSalts(JToken json)
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
                    return ((string)json).StartsWith("**REDACTED**", Globals.STRING_COMPARE_METHOD) ? "**REDACTED**" : GenerateSaltForLeaf();
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

        // static methods //
        private static string GenerateSaltForLeaf()
        {
            Random random = new Random();
            byte[] buffer = new byte[Globals.HASH_ALGORITHM_BLOCK_SIZE];
            random.NextBytes(buffer);
            return HexConverter.ToHex(buffer);
        }

        private static JArray OverrideArrayWithSalts(JArray array)
        {
            JArray result = new JArray();
            foreach (JToken jToken in array)
            {
                result.Add(RecursivelyOverrideJTokenWithSalts(jToken));
            }
            return result;
        }

        private static JObject OverrideObjectWithSalts(JObject obj)
        {
            JObject result = new JObject();
            foreach ((string key, JToken jToken) in obj)
            {
                result[key] = RecursivelyOverrideJTokenWithSalts(jToken);
            }
            return result;
        }
    }
}
