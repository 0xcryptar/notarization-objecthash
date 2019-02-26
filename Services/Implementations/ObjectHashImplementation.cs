﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;
using ObjectHashServer.Models.Extensions;
using ObjectHashServer.Utils;

namespace ObjectHashServer.Services.Implementations
{
    /// <summary>
    /// This is the C# implementation of the ObjectHash libaray from Ben Laurie. 
    /// The source code of may other implementations can be found here: 
    /// https://github.com/benlaurie/objecthash
    /// </summary>
    public class ObjectHashImplementation
    {
        public byte[] Hash { get; private set; }
        private HashAlgorithm digester;
        private MemoryStream memoryStream;

        public ObjectHashImplementation()
        {
            Hash = new byte[Globals.HASH_ALGORITHM_BLOCK_SIZE];
            digester = HashAlgorithm.Create(Globals.HASH_ALGORITHM);
            memoryStream = new MemoryStream();
        }

        /// <summary>
        /// Add any data to the hash calcualtion of the ObjectHashImplementation object.
        /// </summary>
        /// <param name="json">Any valid (RFC 7159 and ECMA-404) JSON data as JToken</param>
        public void HashJToken(JToken json, JToken salts = null)
        {
            switch (json.Type)
            {
                case JTokenType.Array:
                    {
                        try
                        {
                            HashArray((JArray)json, salts.IsNullOrEmpty() ? null : (JArray)salts);
                        } catch (InvalidCastException)
                        {
                            throw new BadRequestException("The provided Salt does not match the JSON object. An array => [] is expected but the Salt data is not of type array");
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
                            throw new BadRequestException("The provided Salt does not match the JSON object. An object => {} is expected but the Salt data is not of type object");
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
                        throw new BadRequestException($"The provided JSON has an invalid type of {json.Type}. Please remove it.");
                    }
            }
        }

        private void AddTaggedByteArray(char tag, byte[] byteArray, JToken salt = null)
        {
            // copying of byteArrays is quite ugly
            // but there is no nicer way in C# to 
            // join two byte arrays
            byte[] tempHash;
            byte[] merged = new byte[byteArray.Length + 1];
            byteArray.CopyTo(merged, 1);
            merged[0] = (byte)tag;
            tempHash = digester.ComputeHash(merged);

            if (salt != null)
            {
                // validate the salt is hex and block size long
                HexConverter.ValidateStringIsHexAndBlockLength(salt);
                // hash salt to equaly distribut randomness
                ObjectHashImplementation jKeyHash = new ObjectHashImplementation();
                jKeyHash.HashString((string)salt);
                // merge salt and object hash as list
                byte[][] hashList = new byte[2][];
                hashList[0] = jKeyHash.Hash;
                hashList[1] = tempHash;

                HashListOfHashes(hashList, 'l', false);
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
            if (str.StartsWith("**REDACTED**", Globals.STRING_COMPARE_METHOD) && str.Length == 76)
            {
                Hash = HexConverter.HashFromHex(str.Substring(12, str.Length - 12));
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
                throw new BadRequestException("The corresponding JSON object contains an array that is different in size from the Salts array. They need to be equaly long.");
            }

            byte[][] hashList = new byte[array.Count][];
            for (int i = 0; i < array.Count; i++)
            {
                ObjectHashImplementation aElementHash = new ObjectHashImplementation();
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

            foreach (var o in obj)
            {
                if (!salts.IsNullOrEmpty() && !salts.ContainsKey(o.Key))
                {
                    IDictionary additionalExceptionData = new Dictionary<string, object>
                        {
                            { "missingKey", o.Key }
                        };

                    throw new BadRequestException("The provided JSON defines an object which is different from the Salts object. Please check the JSON or the salt data.", additionalExceptionData);
                }

                ObjectHashImplementation jKeyHash = new ObjectHashImplementation();
                jKeyHash.HashString(o.Key);
                // TODO: check if acceptable
                // object keys are not salted, i don't see a big issue
                // alternative would be
                // jKeyHash.HashString(SALT + o.Key);
                // but its quite difficult for an user to 
                // store the salts for object keys

                ObjectHashImplementation jValHash = new ObjectHashImplementation();
                jValHash.HashJToken(o.Value, salts.IsNullOrEmpty() ? null : salts[o.Key]);

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
                Array.Sort(hashList, (x, y) => string.Compare(HexConverter.ToHex(x), HexConverter.ToHex(y), Globals.STRING_COMPARE_METHOD));
            }

            memoryStream.Flush();
            memoryStream.WriteByte((byte)type);
            for (int i = 0; i < hashList.GetLength(0); i++)
            {
                memoryStream.Write(hashList[i]);
            }
            Hash = digester.ComputeHash(memoryStream.ToArray());
        }

        private string DebugString()
        {
            return HexConverter.ToHex(memoryStream.ToArray());
        }

        public override string ToString()
        {
            return DebugString();
        }

        public int CompareTo(ObjectHashImplementation other)
        {
            return string.Compare(HashAsString(), other.HashAsString(), Globals.STRING_COMPARE_METHOD);
        }

        public string HashAsString()
        {
            return HexConverter.ToHex(Hash);
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
