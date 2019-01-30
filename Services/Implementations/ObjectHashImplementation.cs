using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;

namespace ObjectHashServer.Services.Implementations
{
    /// <summary>
    /// This is the C# implementation of the ObjectHash libaray from Ben Laurie. 
    /// The source code of may other implementations can be found here: 
    /// https://github.com/benlaurie/objecthash
    /// </summary>
    public class ObjectHashImplementation
    {
        // constants
        private static readonly string HASH_ALGORITHM = "SHA-256";
        private static readonly int HASH_ALGORITHM_BLOCK_SIZE = 32;
        private static readonly bool SORT_ARRAY = false;
        private static readonly StringComparison STRING_COMPARE_METHOD = StringComparison.Ordinal;
        private static readonly NormalizationForm STRING_NORMALIZATION = NormalizationForm.FormC;

        // private variables
        private byte[] hash;
        private HashAlgorithm digester;
        private MemoryStream memoryStream;

        public ObjectHashImplementation()
        {
            hash = new byte[HASH_ALGORITHM_BLOCK_SIZE];
            digester = HashAlgorithm.Create(HASH_ALGORITHM);
            memoryStream = new MemoryStream();
        }

        /// <summary>
        /// Add any data to the hash calcualtion of the ObjectHashImplementation object.
        /// </summary>
        /// <param name="json">Any valid (RFC 7159 and ECMA-404) JSON data as JToken</param>
        public void HashJToken(JToken json)
        {
            switch (json.Type)
            {
                case JTokenType.Array:
                    {
                        HashArray((JArray)json);
                        break;
                    }
                case JTokenType.Object:
                    {
                        HashObject((JObject)json);
                        break;
                    }
                case JTokenType.Integer:
                    {
                        HashLong((long)json);
                        break;
                    }
                case JTokenType.String:
                case JTokenType.TimeSpan:
                case JTokenType.Guid:
                case JTokenType.Uri:
                    {
                        HashString((string)json);
                        break;
                    }
                case JTokenType.Null: 
                case JTokenType.None:
                    {
                        HashNull();
                        break;
                    }
                case JTokenType.Boolean:
                    {
                        HashBoolean((bool)json);
                        break;
                    }
                case JTokenType.Float:
                    {
                        // TODO: check if not to use float instead of double
                        HashDouble((double)json);
                        break;
                    }
                case JTokenType.Bytes:
                    {
                        HashBytes((byte[])json);
                        break;
                    }
                case JTokenType.Date:
                    {
                        HashDateTime((DateTime)json);
                        break;
                    }
                default:
                    {
                        throw new BadRequestException($"The provided JSON has an invalid type of {json.Type}. Please remove it.");
                    }
            }
        }

        private void AddTaggedByteArray(char tag, byte[] byteArray)
        {
            byte[] merged = new byte[byteArray.Length + 1];
            byteArray.CopyTo(merged, 1);
            merged[0] = (byte)tag;
            hash = digester.ComputeHash(merged);
        }

        private void AddTaggedString(char tag, string value)
        {
            AddTaggedByteArray(tag, Encoding.UTF8.GetBytes(value));
        }

        private void HashString(string str)
        {
            if (str.StartsWith("**REDACTED**", STRING_COMPARE_METHOD) && str.Length == 76)
            {
                hash = FromHex(str.Substring(12, str.Length - 12));
            }
            else
            {
                AddTaggedString('u', str.Normalize(STRING_NORMALIZATION));
            }
        }

        private void HashLong(long value)
        {
            AddTaggedString('i', value.ToString());
        }

        private void HashDouble(double value)
        {
            AddTaggedString('f', NormalizeDouble(value));
        }

        private void HashNull()
        {
            AddTaggedString('n', "");
        }

        private void HashBoolean(bool b)
        {
            AddTaggedString('b', b ? "1" : "0");
        }

        private void HashDateTime(DateTime t)
        {
            // normalize DateTime to UTC and ISO 8601
            AddTaggedString('t', t.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        private void HashBytes(byte[] bs)
        {
            // TODO: think about if 'l' is a good tag
            AddTaggedByteArray('l', bs);
        }

        private void HashArray(JArray array)
        {
            byte[][] hashList = new byte[array.Count][];
            for (int i = 0; i < array.Count; i++)
            {
                ObjectHashImplementation aElementHash = new ObjectHashImplementation();
                aElementHash.HashJToken(array[i]);
                hashList[i] = aElementHash.HashAsByteArray();
            }

            // sorting arrays can be useful, but the default should be not to sort arrays
            HashListOfHashes(hashList, 'l', SORT_ARRAY);
        }

        private void HashObject(JObject obj)
        {
            byte[][] hashList = new byte[obj.Count][];
            int i = 0;
            foreach (var o in obj)
            {
                ObjectHashImplementation jKeyHash = new ObjectHashImplementation();
                jKeyHash.HashString(o.Key);

                ObjectHashImplementation jValHash = new ObjectHashImplementation();
                jValHash.HashJToken(o.Value);

                // merge both hashes (of key and value)
                hashList[i] = jKeyHash.HashAsByteArray().Concat(jValHash.HashAsByteArray()).ToArray();
                i++;
            }

            // objects should always be sorted
            HashListOfHashes(hashList, 'd', true);
        }

        private void HashListOfHashes(byte[][] hashList, char type, bool sortArray = false)
        {
            // sorting, if wanted
            if (sortArray)
            {
                Array.Sort(hashList, (x, y) => string.Compare(ToHex(x), ToHex(y), STRING_COMPARE_METHOD));
            }

            // hashing
            memoryStream.Flush();
            memoryStream.WriteByte((byte)type);
            for (int i = 0; i < hashList.GetLength(0); i++)
            {
                memoryStream.Write(hashList[i]);
            }
            hash = digester.ComputeHash(memoryStream.ToArray());
        }

        private string DebugString()
        {
            return ToHex(memoryStream.ToArray());
        }

        public override string ToString()
        {
            // return the hex representation of the current memory stream
            return DebugString();
        }

        public int CompareTo(ObjectHashImplementation other)
        {
            return string.Compare(HashAsString(), other.HashAsString(), STRING_COMPARE_METHOD);
        }

        public byte[] HashAsByteArray()
        {
            return hash;
        }

        public string HashAsString()
        {
            return ToHex(HashAsByteArray());
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

        private static byte[] FromHex(string hex)
        {
            try { 
                return Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();
            } catch(FormatException e)
            {
                throw new BadRequestException("The provided redact hash does not contain a valid SHA256 (64 char, hex only)", e);
            }
        }

        /// <summary>
        /// Normalizes a float/double. This function was taken from benlaurie/objecthash
        /// </summary>
        /// <returns>String of the normalized double</returns>
        /// <param name="d">Input value</param>
        #pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
        private static string NormalizeDouble(double d)
        {
            // Early out for zero. No epsilon diff check wanted.
            if (d == 0.0)
            {
                return "+0:";
            }
            StringBuilder sb = new StringBuilder();
            // Sign
            sb.Append(d < 0.0 ? '-' : '+');
            if (d < 0.0) d = -d;
            // Exponent
            int e = 0;
            while (d > 1)
            {
                d /= 2;
                e += 1;
            }
            while (d < 0.5)
            {
                d *= 2;
                e -= 1;
            }
            sb.Append(e);
            sb.Append(':');
            // Mantissa
            if (d > 1 || d <= 0.5)
            {
                throw new Exception("wrong range for mantissa");
            }
            while (d != 0)
            {
                if (d >= 1)
                {
                    sb.Append('1');
                    d -= 1;
                }
                else
                {
                    sb.Append('0');
                }
                if (d >= 1)
                {
                    throw new Exception("oops, f is too big");
                }
                if (sb.Length > 1000)
                {
                    throw new Exception("things have got out of hand");
                }
                d *= 2;
            }
            return sb.ToString();
        }
        #pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
    }
}
