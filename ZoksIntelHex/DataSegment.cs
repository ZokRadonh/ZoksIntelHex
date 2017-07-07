using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZoksIntelHex
{
    public class DataSegment
    {
        public uint StartAddress { get; set; }
        public List<byte> Bytes { get; set; } = new List<byte>();
        public uint EndAddress => (uint) (StartAddress + Bytes.Count);

        internal string ToIntelHex()
        {
            var str = new StringBuilder((int) (Bytes.Count * 2.5)); // each byte doubled + 24% overhead (doubled again)
            for (var i = 0; i < Bytes.Count; i+=0x10)
            {
                // TODO: Support I16HEX and I32HEX
                var lineNumBytes = i >= Bytes.Count-0x10 ? Bytes.Count%0x10 : 0x10;
                var lineAddress = StartAddress + i;
                var lineBytes = Bytes.Skip(i).Take(lineNumBytes).ToArray();
                var checksum =
                    Utility.CaluclateChecksum(new List<byte>
                    {
                        (byte) lineNumBytes,
                        (byte) (lineAddress & 0xFF),
                        (byte) (lineAddress >> 8),
                        (byte) lineBytes.Sum(b => b) // the checksum algorithm will sum it anyway
                    });
                str.AppendLine(
                    $":{lineNumBytes:X2}{lineAddress:X4}00{string.Concat(lineBytes.Select(b => b.ToString("X2")))}{checksum:X2}");
            }
            return str.ToString();
        }
    }
}
