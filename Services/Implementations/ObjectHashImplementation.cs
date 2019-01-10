using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ObjectHashServer.Services.Implementations
{
    /// <summary>
    /// This is the C# implementation of the original ObjectHash libaray
    /// from Ben Laurie. The source code of may other implementations can 
    /// be found here: https://github.com/benlaurie/objecthash
    /// </summary>
    public class ObjectHashImplementation : IComparable<ObjectHashImplementation>
    {
        // constants
        private static int SHA256_BLOCK_SIZE = 32;
        private static string SHA256 = "SHA-256";

        // private variables
        private byte[] hash;
        private MemoryStream memoryStream;
        private HashAlgorithm digester;

        public ObjectHashImplementation()
        {
            hash = new byte[SHA256_BLOCK_SIZE];
            memoryStream = new MemoryStream();
            digester = HashAlgorithm.Create(SHA256);
        }

        /// <summary>
        /// Hashs any.
        /// </summary>
        /// <param name="json">Any JSON data as JToken</param>
        public void HashAny(JToken json)
        {
            hash = digester.ComputeHash(Encoding.UTF8.GetBytes(json.ToString()));
            return;

            /*
            string[] array = { "foo", "bar" };
            JArray testMe = new JArray(array);
            HashList(testMe);
            return;


            switch (json.Type)
            {
                case JTokenType.Array:
                {
                    HashList((JArray)json);
                    break;
                }
                case JTokenType.Object:
                {
                    HashObject((JObject)json);
                    break;
                }
                case JTokenType.Integer:
                {
                    HashInteger((int)json);
                    break;
                }
                case JTokenType.String:
                {
                    HashString((string)json);
                    break;
                }
                case JTokenType.Null:
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
                    HashDouble((double)json);
                    break;
                }
                default:
                {
                    throw new Exception($"The provided JSON {json.Type} has an invalid type");
                }
            } */
        }

        private void AddTaggedString(char tag, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] merged = new byte[bytes.Length + 1];
            bytes.CopyTo(merged, 1);
            merged[0] = (byte)tag;
            hash = digester.ComputeHash(merged);
        }

        private void HashString(string str)
        {
            AddTaggedString('u', str);
        }

        private void HashInteger(int value)
        {
            AddTaggedString('i', value.ToString());
        }

        private void HashDouble(double value)
        {
            AddTaggedString('f', NormalizeFloat(value));
        }

        private void HashNull()
        {
            AddTaggedString('n', "");
        }

        private void HashBoolean(bool b)
        {
            AddTaggedString('b', b ? "1" : "0");
        }

        private void HashList(JArray list)
        {
            memoryStream.Flush();
            memoryStream.WriteByte((byte)'l');
            for (int i = 0; i < list.Count; i++)
            {
                // create the ObjectHash instances, one for each element in the array
                ObjectHashImplementation objectHash = new ObjectHashImplementation();
                objectHash.HashString((string)list[i]);
                memoryStream.Write(objectHash.HashAsByteArray());
            }
            hash = digester.ComputeHash(memoryStream.ToArray());
        }

        /*           
        private void HashObject(JObject obj)
        {
            List<MemoryStream> buffers = new List<MemoryStream>();
            foreach (var o in obj)
            {
                MemoryStream buff = new MemoryStream(2 * SHA256_BLOCK_SIZE);
                BinaryWriter writer = new BinaryWriter(buff);
                string key = o.Key;
                // would be nice to chain all these calls builder-stylee.
                ObjectHashImplementation hKey = new ObjectHashImplementation();
                hKey.HashString(key);
                ObjectHashImplementation hVal = new ObjectHashImplementation();
                obj.TryGetValue(key, out var value);
                hVal.HashAny(value);
                writer.Write((byte[])(object)hKey.Hash());
                writer.Write((byte[])(object)hVal.Hash());
                buffers.Add(buff);
            }
            buffers = buffers.OrderBy(x => ToHex(Array.ConvertAll(x.ToArray(), b => unchecked((sbyte)b)))).ToList();
            var byteArrays = buffers.Select(x => x.ToArray());
            byte[] merged = new byte[byteArrays.Sum(x => x.Length) + 1];
            merged[0] = (byte)'l';
            int dstOffset = 1;
            foreach (var byteArray in byteArrays)
            {
                Buffer.BlockCopy(byteArray, 0, merged, dstOffset, byteArray.Length);
                dstOffset += byteArray.Length;
            }
            hash = (sbyte[])(Array)digester.ComputeHash(merged);
        } 

        private string DebugString(IEnumerable<MemoryStream> buffers)
        {
            StringBuilder tmp = new StringBuilder();
            foreach (MemoryStream buff in buffers)
            {
                tmp.Append('\n');
                tmp.Append(ToHex(Array.ConvertAll(buff.ToArray(), b => unchecked((sbyte)b))));
            }
            return tmp.ToString();
        } */

        private static int ParseHex(char digit)
        {
            // TODO: Xunit.Assert.True((digit >= '0' && digit <= '9') || (digit >= 'a' && digit <= 'f'));
            if (digit >= '0' && digit <= '9')
            {
                return digit - '0';
            }
            else
            {
                return 10 + digit - 'a';
            }
        }

        /*
        public static ObjectHashImplementation FromHex(string hex)
        {
            ObjectHashImplementation h = new ObjectHashImplementation();
            hex = hex.ToLower();
            if (hex.Length % 2 == 1)
            {
                hex = '0' + hex;
            }

            // TODO: maybe just use Int.valueOf(s).intValue()
            int pos = SHA256_BLOCK_SIZE;
            for (int idx = hex.Length; idx > 0; idx -= 2)
            {
                h.hash[--pos] = (sbyte)(16 * ParseHex(hex[idx - 2]) + ParseHex(hex[idx - 1]));
            }
            return h;
        } */

        private static JTokenType GetType(JToken json)
        {
            return json.Type;
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null) return false;
            if (GetType() != obj.GetType()) return false;
            return HashAsString().Equals(((ObjectHashImplementation)obj).HashAsString());
        }

        public override int GetHashCode()
        {
            // TODO: implement
            return 1;
        }

        public int CompareTo(ObjectHashImplementation other)
        {
            return string.Compare(HashAsString(), other.HashAsString(), StringComparison.Ordinal);
        } 

        public override string ToString()
        {
            // TODO: implement
            return memoryStream.ToString();
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
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        #pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
        static string NormalizeFloat(double f)
        {
            // Early out for zero. No epsiolon diff check wanted.
            if (f == 0.0)
            {
                return "+0:";
            }
            StringBuilder sb = new StringBuilder();
            // Sign
            sb.Append(f < 0.0 ? '-' : '+');
            if (f < 0.0) f = -f;
            // Exponent
            int e = 0;
            while (f > 1)
            {
                f /= 2;
                e += 1;
            }
            while (f < 0.5)
            {
                f *= 2;
                e -= 1;
            }
            sb.Append(e);
            sb.Append(':');
            // Mantissa
            if (f > 1 || f <= 0.5)
            {
                throw new Exception("wrong range for mantissa");
            }
            while (f != 0)
            {
                if (f >= 1)
                {
                    sb.Append('1');
                    f -= 1;
                }
                else
                {
                    sb.Append('0');
                }
                if (f >= 1)
                {
                    throw new Exception("oops, f is too big");
                }
                if (sb.Length > 1000)
                {
                    throw new Exception("things have got out of hand");
                }
                f *= 2;
            }
            return sb.ToString();
        }
        #pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
    }
}
