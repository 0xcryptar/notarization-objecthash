using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;

namespace ObjectHashServer.Services.Implementations
{
    public class GenerateSaltsImplementation
    {
        private static readonly int HASH_ALGORITHM_BLOCK_SIZE = 32;

        public void GenerateSalts(JToken json)
        {
            switch (json.Type)
            {
                case JTokenType.Array:
                    {
                        GenerateSaltsForArray((JArray)json);
                        break;
                    }
                case JTokenType.Object:
                    {
                        GenerateSaltsForObject((JObject)json);
                        break;
                    }
                case JTokenType.Integer:
                case JTokenType.String:
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
                        GenerateSaltForLeaf();
                        break;
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

        private void GenerateSaltsForArray(JArray array)
        {
            // byte[][] hashList = new byte[array.Count][];
            for (int i = 0; i < array.Count; i++)
            {
                GenerateSaltsImplementation aElementHash = new GenerateSaltsImplementation();
                // aElementHash.HashJToken(array[i], salts[i]);
                // hashList[i] = aElementHash.HashAsByteArray();
            }
        }

        private void GenerateSaltsForObject(JObject obj)
        {
            byte[][] hashList = new byte[obj.Count][];

            foreach (var o in obj)
            {
                GenerateSaltsImplementation jKeyHash = new GenerateSaltsImplementation();
                // jKeyHash.HashString(o.Key);

                GenerateSaltsImplementation jValHash = new GenerateSaltsImplementation();
                // jValHash.HashJToken(o.Value, salts[o.Key]);
            }
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
