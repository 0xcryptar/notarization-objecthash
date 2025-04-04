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
                default:
                    throw new ArgumentException($"Algorithm {algorithm} not supported.");
            }
        }
    }

    public enum HashAlgorithmType
    {
        SHA256
    }
}
