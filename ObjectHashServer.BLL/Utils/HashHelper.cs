using System;
using System.Security.Cryptography;

namespace ObjectHashServer.BLL.Utils
{
    public static class HashHelper
    {
        public static HashAlgorithm CreateHashAlgorithm(HashAlgorithmType algorithm)
        {
            switch (algorithm)
            {
                case HashAlgorithmType.SHA256:
                    return SHA256.Create();
                case HashAlgorithmType.SHA512:
                    return SHA512.Create();
                case HashAlgorithmType.BLAKE3:
                    return Blake3Algorithm.Create();
                default:
                    throw new ArgumentException($"Algorithm {algorithm} not supported.");
            }
        }

        public static HashAlgorithmType ParseHashAlgorithmType(string algorithm)
        {
            if (string.IsNullOrEmpty(algorithm))
            {
                return HashAlgorithmType.SHA256;
            }

            if (algorithm.EndsWith("-sha512", StringComparison.OrdinalIgnoreCase))
            {
                return HashAlgorithmType.SHA512;
            }
            else if (algorithm.EndsWith("-blake3", StringComparison.OrdinalIgnoreCase))
            {
                return HashAlgorithmType.BLAKE3;
            }
            else
            {
                return HashAlgorithmType.SHA256;
            }
        }
    }

    public enum HashAlgorithmType
    {
        SHA256,
        SHA512,
        BLAKE3
    }
}
