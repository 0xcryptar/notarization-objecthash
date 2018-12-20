using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
 * This is a C# implementation of the original ObjectHash
 * libaray from Ben Laurie. The source code of may
 * other implementations can be found here:
 * 
 * https://github.com/benlaurie/objecthash
 */
namespace ObjectHashServer.Services.Implementations
{
    public class ObjectHashImplementation : IComparable<ObjectHashImplementation>
    {
        // constants
        private static int SHA256_BLOCK_SIZE = 32;
        private static string SHA256 = "SHA-256";

        // private variables
        private sbyte[] hash;
        private HashAlgorithm digester;

        public ObjectHashImplementation()
        {
            hash = new sbyte[SHA256_BLOCK_SIZE];
            digester = HashAlgorithm.Create(SHA256);
        }

        // hash enty point
        public void HashAny(object obj)
        {
            hash = (sbyte[])(Array)digester.ComputeHash(Encoding.UTF8.GetBytes(obj.ToString()));
            return;

            // TODO: finish
            /*
            JsonType outerType = GetType(obj);
            switch (outerType)
            {
                case JsonType.ARRAY:
                {
                    HashList((JArray)obj);
                    break;
                }
                case JsonType.OBJECT:
                {
                    HashObject((JObject)obj);
                    break;
                }
                case JsonType.INT:
                {
                    HashInteger(obj);
                    break;
                }
                case JsonType.STRING:
                {
                    HashString((string)obj);
                    break;
                }
                case JsonType.NULL:
                {
                    HashNull();
                    break;
                }
                case JsonType.BOOLEAN:
                {
                    HashBoolean((bool)obj);
                    break;
                }
                case JsonType.FLOAT:
                {
                    HashDouble((double)obj);
                    break;
                }
                default:
                {
                    throw new Exception("Illegal type in JSON: " + obj.GetType());
                }
            } */
        }

        private void HashTaggedBytes(char tag, byte[] bytes)
        {
            byte[] merged;
            if (bytes == null)
            {
                merged = new byte[1];
            }
            else
            {
                merged = new byte[bytes.Length + 1];
            }
            hash = (sbyte[])(Array)digester.ComputeHash(merged);
        }

        private void HashString(string str)
        {
            HashTaggedBytes('u', Encoding.UTF8.GetBytes(str));
        }

        private void HashInteger(object value)
        {
            string str = value.ToString();
            HashTaggedBytes('i', Encoding.UTF8.GetBytes(str));
        }

        private void HashDouble(double value)
        {
            string normalized = NormalizeFloat(value);
            HashTaggedBytes('f', Encoding.UTF8.GetBytes(normalized));
        }

        private void HashNull()
        {
            HashTaggedBytes('n', Encoding.UTF8.GetBytes(""));
        }

        private void HashBoolean(bool b)
        {
            HashTaggedBytes('b', Encoding.UTF8.GetBytes(b ? "1" : "0"));
        }

        private void HashList(JArray list)
        {
            var objectHashList = new List<ObjectHashImplementation>();
            for (int n = 0; n < list.Count; ++n)
            {
                ObjectHashImplementation innerObject = new ObjectHashImplementation();
                innerObject.HashAny(list[n]);
                objectHashList.Add(innerObject);
            }
            byte[] merged = new byte[objectHashList.Sum(x => x.hash?.Length ?? 0) + 1];
            merged[0] = (byte)'l';
            int dstOffset = 1;
            foreach (var objectHash in objectHashList)
            {
                int sourceCount = objectHash.hash?.Length ?? 0;
                Buffer.BlockCopy(objectHash.hash, 0, merged, dstOffset, sourceCount);
                dstOffset += sourceCount;
            }
            hash = (sbyte[])(Array)digester.ComputeHash(merged);
        }

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
            StringBuilder sb = new StringBuilder();
            foreach (MemoryStream buff in buffers)
            {
                sb.Append('\n');
                sb.Append(ToHex(Array.ConvertAll(buff.ToArray(), b => unchecked((sbyte)b))));
            }
            return sb.ToString();
        }

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
        }

        private static JTokenType GetType(JObject jsonObj)
        {
            return jsonObj.Type;

            // TODO: check if needed
            /*
            if (jsonObj is null || (jsonObj as JValue)?.Type == JTokenType.Null)
            {
                return JsonType.NULL;
            }
            else if (jsonObj is JArray)
            {
                return JsonType.ARRAY;
            }
            else if (jsonObj is JObject)
            {
                return JsonType.OBJECT;
            }
            else if (jsonObj is string)
            {
                return JsonType.STRING;
            }
            else if (jsonObj is int || jsonObj is long)
            {
                return JsonType.INT;
            }
            else if (jsonObj is double)
            {
                return JsonType.FLOAT;
            }
            else if (jsonObj is bool)
            {
                return JsonType.BOOLEAN;
            }
            else
            {
                Console.WriteLine("jsonObj is_a " + jsonObj.GetType());
                return JsonType.UNKNOWN;
            }
            */
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null) return false;
            if (GetType() != obj.GetType()) return false;
            return ToHex().Equals(((ObjectHashImplementation)obj).ToHex());
        }

        public override int GetHashCode()
        {
            // TODO: implement
            return 1;
        }

        public int CompareTo(ObjectHashImplementation other)
        {
            return string.Compare(ToHex(), other.ToHex(), StringComparison.Ordinal);
        }

        public sbyte[] Hash()
        {
            return hash;
        }

        public override string ToString()
        {
            return ToHex();
        }

        public string ToHex()
        {
            return ToHex(hash);
        }

        private static string ToHex(sbyte[] buffer)
        {
            StringBuilder hexString = new StringBuilder();
            for (int idx = 0; idx < buffer.Length; ++idx)
            {
                string hex = (0xff & buffer[idx]).ToString("X");
                if (hex.Length == 1)
                {
                    hexString.Append('0');
                }
                hexString.Append(hex);
            }
            return hexString.ToString();
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
