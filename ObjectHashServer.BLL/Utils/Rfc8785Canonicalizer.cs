using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Exceptions;

namespace ObjectHashServer.BLL.Utils
{
    /// <summary>
    /// Implements RFC 8785 (JSON Canonicalization Scheme - JCS).
    /// Generates canonical UTF-8 JSON text and SHA-256 hashes according to RFC 8785.
    /// </summary>
    public static class Rfc8785Canonicalizer
    {
        public static string Canonicalize(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.None)
            {
                return "null";
            }

            StringBuilder sb = new StringBuilder();
            SerializeToken(token, sb);
            return sb.ToString();
        }

        public static byte[] ComputeHash(JToken token, HashAlgorithmType algorithmType = HashAlgorithmType.SHA256)
        {
            string canonicalJsonStr = Canonicalize(token);
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(canonicalJsonStr);
            using HashAlgorithm digester = HashHelper.CreateHashAlgorithm(algorithmType);
            return digester.ComputeHash(utf8Bytes);
        }

        private static void SerializeToken(JToken token, StringBuilder sb)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    SerializeObject((JObject)token, sb);
                    break;
                case JTokenType.Array:
                    SerializeArray((JArray)token, sb);
                    break;
                case JTokenType.String:
                case JTokenType.TimeSpan:
                case JTokenType.Guid:
                case JTokenType.Uri:
                    SerializeString((string)token, sb);
                    break;
                case JTokenType.Integer:
                    if (token is JValue jVal && jVal.Value is long longVal)
                    {
                        sb.Append(longVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(JcsNumberFormatter.FormatNumber((double)token));
                    }
                    break;
                case JTokenType.Float:
                    sb.Append(JcsNumberFormatter.FormatNumber((double)token));
                    break;
                case JTokenType.Boolean:
                    sb.Append((bool)token ? "true" : "false");
                    break;
                case JTokenType.Null:
                case JTokenType.None:
                    sb.Append("null");
                    break;
                case JTokenType.Date:
                    SerializeString(((DateTime)token).ToString("yyyy-MM-ddTHH:mm:ssZ"), sb);
                    break;
                default:
                    throw new BadRequestException($"JSON type {token.Type} is not supported by RFC 8785.");
            }
        }

        private static void SerializeObject(JObject obj, StringBuilder sb)
        {
            sb.Append('{');
            var sortedProperties = obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
            for (int i = 0; i < sortedProperties.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                SerializeString(sortedProperties[i].Name, sb);
                sb.Append(':');
                SerializeToken(sortedProperties[i].Value, sb);
            }
            sb.Append('}');
        }

        private static void SerializeArray(JArray arr, StringBuilder sb)
        {
            sb.Append('[');
            for (int i = 0; i < arr.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                SerializeToken(arr[i], sb);
            }
            sb.Append(']');
        }

        private static void SerializeString(string str, StringBuilder sb)
        {
            sb.Append('"');
            if (str != null)
            {
                foreach (char c in str)
                {
                    switch (c)
                    {
                        case '"':
                            sb.Append("\\\"");
                            break;
                        case '\\':
                            sb.Append("\\\\");
                            break;
                        case '\b':
                            sb.Append("\\b");
                            break;
                        case '\t':
                            sb.Append("\\t");
                            break;
                        case '\n':
                            sb.Append("\\n");
                            break;
                        case '\f':
                            sb.Append("\\f");
                            break;
                        case '\r':
                            sb.Append("\\r");
                            break;
                        default:
                            if (c < 0x20)
                            {
                                sb.Append($"\\u{(int)c:x4}");
                            }
                            else
                            {
                                sb.Append(c);
                            }
                            break;
                    }
                }
            }
            sb.Append('"');
        }
    }
}
