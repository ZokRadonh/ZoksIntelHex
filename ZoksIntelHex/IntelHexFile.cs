using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ZoksIntelHex
{
    /// <summary>
    /// Supports read(I8HEX, I16HEX, I32HEX) and write(I8HEX) of Intel Hex Format files.
    /// </summary>
    public class IntelHexFile
    {
        /// <summary>
        /// Data segments that are read by ReadIntelHex or that should be used for CreateIntelHex.
        /// </summary>
        public List<DataSegment> DataSegments { get; } = new List<DataSegment>();

        public ushort CS { get; set; }
        public ushort IP { get; set; }
        public uint EIP { get; set; }

        private uint _currentAddress = 0;

        /// <summary>
        /// Create Intel hex format string of data in DataSegments.
        /// </summary>
        /// <returns></returns>
        public string CreateIntelHex()
        {
            var data = string.Concat(DataSegments.Select(segment => segment.ToIntelHex()));
            return data + ":00000001FF\r\n"; // add eof record
        }

        /// <summary>
        /// Read Intel hex format data.
        /// </summary>
        /// <param name="stream"></param>
        public void ReadIntelHex(Stream stream)
        {
            var r = new StreamReader(stream);
            var line = r.ReadLine();
            while (line != null)
            {
                try
                {
                    if (line[0] != ':')
                    {
                        throw new InvalidDataException($"Invalid data format. Expected ':' instead of '{line[0]}'.");
                    }
                    int dataLen;
                    if (!int.TryParse(line.Substring(1, 2), NumberStyles.HexNumber, new NumberFormatInfo(), out dataLen))
                    {
                        throw new InvalidDataException($"Invalid data format. Expected line length number instead of '{line.Substring(1,2)}'.");
                    }
                    int recordAddress;
                    if (!int.TryParse(line.Substring(3, 4), NumberStyles.HexNumber, new NumberFormatInfo(), out recordAddress))
                    {
                        throw new InvalidDataException($"Invalid data format. Expected record address instead of '{line.Substring(3, 4)}'.");
                    }
                    int recordType;
                    if (!int.TryParse(line.Substring(7, 2), NumberStyles.HexNumber, new NumberFormatInfo(), out recordType))
                    {
                        throw new InvalidDataException($"Invalid data format. Expected record type instead of '{line.Substring(7, 2)}'.");
                    }
                    var dataAscii = line.Substring(9, dataLen*2);
                    byte checkSum;
                    if (!byte.TryParse(line.Substring(9 + dataLen * 2, 2), NumberStyles.HexNumber, new NumberFormatInfo(), out checkSum))
                    {
                        throw new InvalidDataException($"Invalid data format. Expected checksum instead of '{line.Substring(9 + dataLen * 2, 2)}'.");
                    }

                    try
                    {
                        // get data as bytes
                        var dataBytes = Enumerable.Range(0, dataLen)
                            .Select(i => Convert.ToByte(dataAscii.Substring(i * 2, 2), 16)).ToArray();
                        
                        // verify checksum
                        var allData = new List<byte>
                        {
                            (byte) dataLen,
                            (byte) recordType,
                            (byte) (recordAddress & 0xFF),
                            (byte) (recordAddress >> 8)
                        };
                        allData.AddRange(dataBytes);
                        var caluclatedChecksum = Utility.CaluclateChecksum(allData);
                        if (checkSum != caluclatedChecksum)
                        {
                            throw new InvalidDataException($"Invalid record checksum. Expected '{checkSum}' but calculated '{caluclatedChecksum}'.");
                        }

                        // read record
                        switch (recordType)
                        {
                            case 0: // Data
                                GetSegment((uint) (recordAddress+_currentAddress)).Bytes.AddRange(dataBytes);
                                break;
                            case 1: // End of file
                                return;
                            case 2: // Extended Segment Address
                                _currentAddress = Convert.ToUInt16(dataAscii, 16) * 16U;
                                break;
                            case 3: // Start Segment Address
                                CS = Convert.ToUInt16(dataAscii.Substring(0, 2), 16);
                                IP = Convert.ToUInt16(dataAscii.Substring(2, 2), 16);
                                break;
                            case 4: // Extended Linear Address
                                _currentAddress = Convert.ToUInt32(dataAscii, 16) << 16;
                                break;
                            case 5: // Start Linear Address
                                EIP = Convert.ToUInt32(dataAscii.Substring(0, 4), 16);
                                break;
                            default:
                                throw new InvalidDataException($"Invalid data format. Unknown record type: '{recordType:D2}'.");
                        }
                    }
                    catch (FormatException)
                    {
                        throw new InvalidDataException($"Invalid data format. Data payload is invalid: '{dataAscii}'.");
                    }

                    line = r.ReadLine();
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new InvalidDataException($"Invalid data format. Unexpected end of line: '{line}'.");
                }
            }
        }

        /// <summary>
        /// Get appropriate segment or create new segment.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private DataSegment GetSegment(uint address)
        {
            var seg =
                DataSegments.FirstOrDefault(segment => segment.StartAddress < address && segment.EndAddress >= address);
            if (seg == null)
            {
                seg = new DataSegment { StartAddress = address };
                DataSegments.Add(seg);
            }
            return seg;
        }
    }
}
