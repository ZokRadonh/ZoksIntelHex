using System.Collections.Generic;
using System.Linq;

namespace ZoksIntelHex
{
    internal static class Utility
    {
        public static byte CaluclateChecksum(IEnumerable<byte> bytes)
        {
            return (byte) ((bytes.Sum(b => b) ^ 0xFF) + 1);
        }
    }
}
