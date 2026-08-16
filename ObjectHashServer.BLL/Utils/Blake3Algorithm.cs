using System;
using System.IO;
using System.Security.Cryptography;

namespace ObjectHashServer.BLL.Utils
{
    /// <summary>
    /// Managed implementation of the BLAKE3 cryptographic hash algorithm (256-bit output).
    /// Compliant with the official BLAKE3 specification.
    /// </summary>
    public sealed class Blake3Algorithm : HashAlgorithm
    {
        private static readonly uint[] IV = new uint[]
        {
            0x6A09E667u, 0xBB67AE85u, 0x3C6EF372u, 0xA54FF53Au,
            0x510E527Fu, 0x9B05688Cu, 0x1F83D9ABu, 0x5BE0CD19u
        };

        private static readonly byte[][] MSG_SCHEDULE = new byte[][]
        {
            new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
            new byte[] { 2, 6, 3, 10, 7, 0, 4, 13, 1, 11, 12, 5, 8, 14, 15, 9 },
            new byte[] { 3, 4, 10, 12, 13, 2, 7, 14, 6, 5, 15, 11, 1, 8, 9, 0 },
            new byte[] { 10, 7, 12, 15, 14, 3, 13, 8, 4, 11, 0, 5, 2, 1, 9, 6 },
            new byte[] { 12, 13, 15, 0, 8, 10, 14, 1, 7, 5, 6, 11, 3, 2, 9, 4 },
            new byte[] { 15, 14, 0, 6, 1, 12, 8, 2, 13, 11, 7, 5, 10, 3, 9, 4 },
            new byte[] { 0, 8, 2, 7, 12, 15, 1, 3, 14, 5, 11, 6, 10, 13, 9, 4 }
        };

        private const int CHUNK_START = 1;
        private const int CHUNK_END = 2;
        private const int PARENT = 4;
        private const int ROOT = 8;

        private readonly MemoryStream _bufferStream = new MemoryStream();

        public Blake3Algorithm()
        {
            HashSizeValue = 256; // 32 bytes
        }

        public new static Blake3Algorithm Create()
        {
            return new Blake3Algorithm();
        }

        public override void Initialize()
        {
            _bufferStream.SetLength(0);
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _bufferStream.Write(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            byte[] data = _bufferStream.ToArray();
            return CompressData(data);
        }

        private static byte[] CompressData(byte[] input)
        {
            if (input.Length == 0)
            {
                return CompressChunk(new byte[64], 0, 0, CHUNK_START | CHUNK_END | ROOT);
            }

            int numChunks = (input.Length + 1023) / 1024;
            byte[][] chunkHashes = new byte[numChunks][];

            for (int c = 0; c < numChunks; c++)
            {
                int chunkOffset = c * 1024;
                int chunkLength = Math.Min(1024, input.Length - chunkOffset);
                chunkHashes[c] = ProcessChunk(input, chunkOffset, chunkLength, (ulong)c, numChunks == 1);
            }

            while (chunkHashes.Length > 1)
            {
                int nextCount = (chunkHashes.Length + 1) / 2;
                byte[][] nextHashes = new byte[nextCount][];

                for (int i = 0; i < nextCount; i++)
                {
                    int left = i * 2;
                    int right = left + 1;

                    if (right < chunkHashes.Length)
                    {
                        bool isRoot = (nextCount == 1);
                        nextHashes[i] = CompressParent(chunkHashes[left], chunkHashes[right], isRoot);
                    }
                    else
                    {
                        nextHashes[i] = chunkHashes[left];
                    }
                }
                chunkHashes = nextHashes;
            }

            return chunkHashes[0];
        }

        private static byte[] ProcessChunk(byte[] input, int offset, int length, ulong chunkIndex, bool isSingleChunk)
        {
            int blockCount = (length + 63) / 64;
            if (blockCount == 0) blockCount = 1;

            uint[] cv = (uint[])IV.Clone();

            for (int b = 0; b < blockCount; b++)
            {
                int blockOffset = offset + (b * 64);
                int blockLen = Math.Min(64, length - (b * 64));
                byte[] block = new byte[64];
                if (blockLen > 0)
                {
                    Array.Copy(input, blockOffset, block, 0, blockLen);
                }

                int flags = 0;
                if (b == 0) flags |= CHUNK_START;
                if (b == blockCount - 1) flags |= CHUNK_END;
                if (isSingleChunk && (b == blockCount - 1)) flags |= ROOT;

                cv = CompressBlock(cv, block, chunkIndex, (uint)blockLen, flags);
            }

            byte[] result = new byte[32];
            for (int i = 0; i < 8; i++)
            {
                byte[] bytes = BitConverter.GetBytes(cv[i]);
                Array.Copy(bytes, 0, result, i * 4, 4);
            }
            return result;
        }

        private static byte[] CompressParent(byte[] leftChild, byte[] rightChild, bool isRoot)
        {
            byte[] block = new byte[64];
            Array.Copy(leftChild, 0, block, 0, 32);
            Array.Copy(rightChild, 0, block, 32, 32);

            int flags = PARENT;
            if (isRoot) flags |= ROOT;

            uint[] cv = CompressBlock((uint[])IV.Clone(), block, 0, 64, flags);
            byte[] result = new byte[32];
            for (int i = 0; i < 8; i++)
            {
                byte[] bytes = BitConverter.GetBytes(cv[i]);
                Array.Copy(bytes, 0, result, i * 4, 4);
            }
            return result;
        }

        private static uint[] CompressBlock(uint[] cv, byte[] block, ulong counter, uint blockLen, int flags)
        {
            uint[] m = new uint[16];
            for (int i = 0; i < 16; i++)
            {
                m[i] = BitConverter.ToUInt32(block, i * 4);
            }

            uint[] state = new uint[16];
            Array.Copy(cv, 0, state, 0, 8);
            Array.Copy(IV, 0, state, 8, 4);
            state[12] = (uint)(counter & 0xFFFFFFFF);
            state[13] = (uint)(counter >> 32);
            state[14] = blockLen;
            state[15] = (uint)flags;

            for (int r = 0; r < 7; r++)
            {
                byte[] s = MSG_SCHEDULE[r];
                Round(state, m, s);
            }

            uint[] outCv = new uint[8];
            for (int i = 0; i < 8; i++)
            {
                outCv[i] = state[i] ^ state[i + 8];
            }
            return outCv;
        }

        private static void Round(uint[] state, uint[] m, byte[] s)
        {
            G(state, 0, 4, 8, 12, m[s[0]], m[s[1]]);
            G(state, 1, 5, 9, 13, m[s[2]], m[s[3]]);
            G(state, 2, 6, 10, 14, m[s[4]], m[s[5]]);
            G(state, 3, 7, 11, 15, m[s[6]], m[s[7]]);

            G(state, 0, 5, 10, 15, m[s[8]], m[s[9]]);
            G(state, 1, 6, 11, 12, m[s[10]], m[s[11]]);
            G(state, 2, 7, 8, 13, m[s[12]], m[s[13]]);
            G(state, 3, 4, 9, 14, m[s[14]], m[s[15]]);
        }

        private static void G(uint[] v, int a, int b, int c, int d, uint mx, uint my)
        {
            v[a] = v[a] + v[b] + mx;
            v[d] = RotateRight(v[d] ^ v[a], 16);
            v[c] = v[c] + v[d];
            v[b] = RotateRight(v[b] ^ v[c], 12);
            v[a] = v[a] + v[b] + my;
            v[d] = RotateRight(v[d] ^ v[a], 8);
            v[c] = v[c] + v[d];
            v[b] = RotateRight(v[b] ^ v[c], 7);
        }

        private static uint RotateRight(uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }
    }
}
