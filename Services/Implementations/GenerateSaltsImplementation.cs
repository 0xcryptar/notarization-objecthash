using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;
using ObjectHashServer.Models.Api.Request;
using ObjectHashServer.Models.Extensions;

namespace ObjectHashServer.Services.Implementations
{
    public class GenerateSaltsImplementation
    {
        private static readonly int HASH_ALGORITHM_BLOCK_SIZE = 32;

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
                        if(((string)json).StartsWith("**REDACTED**", StringComparison.Ordinal))
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
            byte[] buffer = new byte[HASH_ALGORITHM_BLOCK_SIZE];
            random.NextBytes(buffer);
            return ToHex(buffer);
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

        private static string ToHex(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        private static byte[] HashFromHex(string hash)
        {
            if (hash.Length != (HASH_ALGORITHM_BLOCK_SIZE * 2) || !System.Text.RegularExpressions.Regex.IsMatch(hash, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                throw new BadRequestException($"The provided salt has an invaid length ({HASH_ALGORITHM_BLOCK_SIZE * 2} characters, hex only)");
            }

            return Enumerable.Range(0, hash.Length)
                 .Where(x => x % 2 == 0)
                 .Select(x => Convert.ToByte(hash.Substring(x, 2), 16))
                 .ToArray();
        }
    }
}
