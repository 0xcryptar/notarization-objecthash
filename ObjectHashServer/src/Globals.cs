using System;
using System.Text;

namespace ObjectHashServer
{
    public static class Globals
    {
        public static readonly bool SORT_ARRAY = false;
        // see: https://github.com/benlaurie/objecthash/issues/52
        public static readonly bool COMMON_JSONIFY = true;
        public static readonly string HASH_ALGORITHM = "SHA-256";
        public static readonly int HASH_ALGORITHM_BLOCK_SIZE = 32;
        public static readonly StringComparison STRING_COMPARE_METHOD = StringComparison.Ordinal;
        public static readonly NormalizationForm STRING_NORMALIZATION = NormalizationForm.FormC;
    }
}
