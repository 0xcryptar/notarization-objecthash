using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Exceptions;
using System.Text;

namespace ObjectHashServer.BLL.Utils
{
    public static class HexConverter
    {
        public static string ToHex(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        public static byte[] HashFromHex(string hex)
        {
            ValidateStringIsHexAndBlockLength(hex);
            return Enumerable.Range(0, hex.Length)
                 .Where(x => x % 2 == 0)
                 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                 .ToArray();
        }

        public static void ValidateStringIsHexAndBlockLength(JToken objectVal)
        {
            string hash;
            try
            {
                hash = (string)objectVal;
            }
            catch (InvalidCastException)
            {
                throw new BadRequestException("The provided hash or salt is not a valid string.");
            }

            if (hash.Length != (Globals.HASH_ALGORITHM_BLOCK_SIZE * 2) || !System.Text.RegularExpressions.Regex.IsMatch(hash, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                throw new BadRequestException($"The provided hash or salt is not a valid {Globals.HASH_ALGORITHM} ({Globals.HASH_ALGORITHM_BLOCK_SIZE * 2} characters, hex only)");
            }
        }
    }
}
