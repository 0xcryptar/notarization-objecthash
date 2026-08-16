using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Exceptions;
using ObjectHashServer.BLL.Models.Extensions;
using ObjectHashServer.BLL.Utils;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable PossibleNullReferenceException
// ReSharper disable SuggestBaseTypeForParameter

namespace ObjectHashServer.BLL.Services.Implementations
{
    /// <summary>
    /// This is the C# implementation of the ObjectHash library from Ben Laurie. 
    /// The source code of may other implementations can be found here: 
    /// https://github.com/benlaurie/objecthash
    /// </summary>
    public class ObjectHashImplementation
    {
        private byte[] Hash { get; set; }
        private readonly HashAlgorithm _digester;
        private readonly MemoryStream _memoryStream;
        private readonly HashAlgorithmType _algorithmType;

        public ObjectHashImplementation(HashAlgorithmType algorithm = HashAlgorithmType.SHA256)
        {
            _algorithmType = algorithm;
            _digester = HashHelper.CreateHashAlgorithm(algorithm);
            int digestSizeInBytes = _digester.HashSize / 8;
            Hash = new byte[digestSizeInBytes];
            _memoryStream = new MemoryStream();
        }

        /// <summary>
        /// Add any data to the hash calculation of the ObjectHashImplementation object.
        /// </summary>
        /// <param name="json">Any valid (RFC 7159 and ECMA-404) JSON data as JToken</param>
        /// <param name="salts">Salts fitting to the JSON object.</param>
        public void HashJToken(JToken json, JToken salts = null)
        {
            switch (json.Type)
            {
                case JTokenType.Array:
                    {
                        try
                        {
                            HashArray((JArray)json, salts.IsNullOrEmpty() ? null : (JArray)salts);
                        }
                        catch (InvalidCastException)
                        {
                            throw new BadRequestException(
                                "The provided Salt does not match the JSON object. An array => [] is expected but the Salt data is not of type array");
                        }

                        break;
                    }
                case JTokenType.Object:
                    {
                        try
                        {
                            HashObject((JObject)json, salts.IsNullOrEmpty() ? null : (JObject)salts);
                        }
                        catch (InvalidCastException)
                        {
                            throw new BadRequestException(
                                "The provided Salt does not match the JSON object. An object => {} is expected but the Salt data is not of type object");
                        }

                        break;
                    }
                case JTokenType.String:
                case JTokenType.TimeSpan:
                case JTokenType.Guid:
                case JTokenType.Uri:
                    {
                        HashString((string)json, salts);
                        break;
                    }
                case JTokenType.Null:
                case JTokenType.None:
                    {
                        HashNull(salts);
                        break;
                    }
                case JTokenType.Boolean:
                    {
                        HashBoolean((bool)json, salts);
                        break;
                    }
                case JTokenType.Integer:
                    {
                        if (Globals.COMMON_JSONIFY)
                        {
                            HashDouble((double)json, salts);
                        }
                        else
                        {
                            HashLong((long)json, salts);
                        }

                        break;
                    }
                case JTokenType.Float:
                    {
                        HashDouble((double)json, salts);
                        break;
                    }
                case JTokenType.Bytes:
                    {
                        HashBytes((byte[])json, salts);
                        break;
                    }
                case JTokenType.Date:
                    {
                        HashDateTime((DateTime)json, salts);
                        break;
                    }
                default:
                    {
                        throw new BadRequestException(
                            $"The provided JSON has an invalid type of {json.Type}. Please remove it.");
                    }
            }
        }

        private void AddTaggedByteArray(char tag, byte[] byteArray, JToken salt = null)
        {
            // copying of byteArrays is quite ugly but there is no nicer way in C# to join two byte arrays
            byte[] merged = new byte[byteArray.Length + 1];
            byteArray.CopyTo(merged, 1);
            merged[0] = (byte)tag;
            byte[] tempHash = _digester.ComputeHash(merged);

            if (salt != null)
            {
                // validate the salt is hex
                HexConverter.ValidateStringIsHexAndBlockLength(salt);
                // hash salt to equally distribute randomness
                ObjectHashImplementation jKeyHash = new ObjectHashImplementation(_algorithmType);
                jKeyHash.HashString((string)salt);
                // merge salt and object hash as list
                byte[][] hashList = new byte[2][];
                hashList[0] = jKeyHash.Hash;
                hashList[1] = tempHash;

                HashListOfHashes(hashList, 'l');
            }
            else
            {
                Hash = tempHash;
            }
        }

        private void AddTaggedString(char tag, string value, JToken salt = null)
        {
            AddTaggedByteArray(tag, Encoding.UTF8.GetBytes(value), salt);
        }

        private void HashString(string str, JToken salt = null)
        {
            if (str.StartsWith("**REDACTED**", Globals.STRING_COMPARE_METHOD) && str.Length > 12)
            {
                Hash = HexConverter.HashFromHex(str.Substring(12));
            }
            else
            {
                AddTaggedString('u', str.Normalize(Globals.STRING_NORMALIZATION), salt);
            }
        }

        private void HashLong(long value, JToken salt = null)
        {
            AddTaggedString('i', value.ToString(), salt);
        }

        private void HashDouble(double value, JToken salt = null)
        {
            AddTaggedString('f', NormalizeDouble(value), salt);
        }

        private void HashNull(JToken salt = null)
        {
            AddTaggedString('n', "", salt);
        }

        private void HashBoolean(bool b, JToken salt = null)
        {
            AddTaggedString('b', b ? "1" : "0", salt);
        }

        private void HashDateTime(DateTime t, JToken salt = null)
        {
            // normalize DateTime to UTC and ISO 8601
            AddTaggedString('t', t.ToString("yyyy-MM-ddTHH:mm:ssZ"), salt);
        }

        private void HashBytes(byte[] bs, JToken salt = null)
        {
            // TODO: check if 'l' is a good tag
            AddTaggedByteArray('l', bs, salt);
        }

        private void HashArray(JArray array, JArray salts = null)
        {
            if (!salts.IsNullOrEmpty() && salts.Count != array.Count)
            {
                throw new BadRequestException(
                    "The corresponding JSON object contains an array that is different in size from the Salts array. They need to be equally long.");
            }

            byte[][] hashList = new byte[array.Count][];
            for (int i = 0; i < array.Count; i++)
            {
                ObjectHashImplementation aElementHash = new ObjectHashImplementation(_algorithmType);
                aElementHash.HashJToken(array[i], salts.IsNullOrEmpty() ? null : salts[i]);
                hashList[i] = aElementHash.Hash;
            }

            // sorting arrays can be needed, but the default should be not to sort arrays
            HashListOfHashes(hashList, 'l', Globals.SORT_ARRAY);
        }

        private void HashObject(JObject obj, JObject salts = null)
        {
            byte[][] hashList = new byte[obj.Count][];
            int i = 0;

            foreach ((string key, JToken value) in obj)
            {
                if (!salts.IsNullOrEmpty() && !salts.ContainsKey(key))
                {
                    IDictionary additionalExceptionData = new Dictionary<string, object>
                    {
                        {"missingKey", key}
                    };

                    throw new BadRequestException(
                        "The provided JSON defines an object which is different from the Salts object. Please check the JSON or the salt data.",
                        additionalExceptionData);
                }

                ObjectHashImplementation jKeyHash = new ObjectHashImplementation(_algorithmType);
                jKeyHash.HashString(key);

                ObjectHashImplementation jValHash = new ObjectHashImplementation(_algorithmType);
                jValHash.HashJToken(value, salts.IsNullOrEmpty() ? null : salts[key]);

                // merge both hashes (of key and value)
                hashList[i] = jKeyHash.Hash.Concat(jValHash.Hash).ToArray();
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
                Array.Sort(hashList,
                    (x, y) => string.Compare(HexConverter.ToHex(x), HexConverter.ToHex(y),
                        Globals.STRING_COMPARE_METHOD));
            }

            _memoryStream.Flush();
            _memoryStream.WriteByte((byte)type);
            for (int i = 0; i < hashList.GetLength(0); i++)
            {
                _memoryStream.Write(hashList[i]);
            }

            Hash = _digester.ComputeHash(_memoryStream.ToArray());
        }

        private string DebugString()
        {
            return HexConverter.ToHex(_memoryStream.ToArray());
        }

        public override string ToString()
        {
            return DebugString();
        }

        // ReSharper disable once UnusedMember.Global
        public int CompareTo(ObjectHashImplementation other)
        {
            return string.Compare(HashAsString(), other.HashAsString(), Globals.STRING_COMPARE_METHOD);
        }

        public string HashAsString()
        {
            return HexConverter.ToHex(Hash);
        }

        /// <summary>
        /// Normalizes a float/double according to RFC 8785 (JCS) Section 3.2.2 (ECMAScript ES6 ToString(Number) rules).
        /// This modernizes Ben Laurie's Merkle-tree hashing with standard RFC 8785 number formatting (cryptar-v1-sha256).
        /// </summary>
        /// <returns>String of the normalized double</returns>
        /// <param name="d">Input value</param>
        private static string NormalizeDouble(double d)
        {
            return JcsNumberFormatter.FormatNumber(d);
        }
    }
}