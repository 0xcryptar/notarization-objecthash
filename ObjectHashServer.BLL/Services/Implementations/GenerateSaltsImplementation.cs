using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Exceptions;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Models.Extensions;
using ObjectHashServer.BLL.Utils;
using System.Security.Cryptography;

namespace ObjectHashServer.BLL.Services.Implementations
{
    public static class GenerateSaltsImplementation
    {
        public static void SetRandomSaltsForObjectBaseRequestModel(ObjectBaseRequestModel model)
        {
            if (!model.Salts.IsNullOrEmpty())
            {
                throw new BadRequestException("You want to generate new salts but you send salts with the request. Please either generate new salts or send them with the request.");
            }

            model.Salts = SaltsForJToken(model.Data, model.SaltBitLength);
        }

        public static JToken SaltsForJToken(JToken json, int saltBitLength = 256)
        {
            JToken jsonClone = json.DeepClone();
            return RecursivelyOverrideJTokenWithSalts(jsonClone, saltBitLength);
        }

        private static JToken RecursivelyOverrideJTokenWithSalts(JToken json, int saltBitLength)
        {
            switch (json.Type)
            {
                case JTokenType.Array:
                    {
                        return OverrideArrayWithSalts((JArray)json, saltBitLength);
                    }
                case JTokenType.Object:
                    {
                        return OverrideObjectWithSalts((JObject)json, saltBitLength);
                    }
                case JTokenType.String:
                    {
                        return ((string)json).StartsWith("**REDACTED**", Globals.STRING_COMPARE_METHOD) ? "**REDACTED**" : GenerateSaltForLeaf(saltBitLength);
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
                        return GenerateSaltForLeaf(saltBitLength);
                    }
                default:
                    {
                        throw new BadRequestException($"The provided JSON has an invalid type of {json.Type}. Please remove it.");
                    }
            }
        }

        // static methods //
        private static string GenerateSaltForLeaf(int saltBitLength = 256)
        {
            if (saltBitLength != 128 && saltBitLength != 256)
            {
                throw new BadRequestException("Salt bit length must be either 128 or 256 bits.");
            }

            int byteLength = saltBitLength / 8;
            byte[] buffer = RandomNumberGenerator.GetBytes(byteLength);
            return HexConverter.ToHex(buffer);
        }

        private static JArray OverrideArrayWithSalts(JArray array, int saltBitLength)
        {
            JArray result = new JArray();
            foreach (JToken jToken in array)
            {
                result.Add(RecursivelyOverrideJTokenWithSalts(jToken, saltBitLength));
            }
            return result;
        }

        private static JObject OverrideObjectWithSalts(JObject obj, int saltBitLength)
        {
            JObject result = new JObject();
            foreach ((string key, JToken jToken) in obj)
            {
                result[key] = RecursivelyOverrideJTokenWithSalts(jToken, saltBitLength);
            }
            return result;
        }
    }
}
