using System.Collections.Generic;

namespace Shiningforce
{
    public class Decompression
    {
        public static byte[] DecompressData(byte[] data, int startOffset)
        {
            List<byte> decompressedData = new List<byte>();

            int readPos = startOffset;
            int currentPos = 0;

            while (true)
            {
                int bitfield = ByteArray.GetInt16(data, readPos);
                readPos += 2;

                for (int i = 0; i < 16; i++)
                {
                    bool commandFlag = (bitfield & 0x8000) != 0;

                    if (commandFlag)
                    {
                        int value = ByteArray.GetInt16(data, readPos);
                        readPos += 2;

                        if (value == 0)
                        {
                            // end of Stream command
                            return decompressedData.ToArray();
                        }

                        int negativeOffset = (value >> 5) * 2;
                        int count = ((value & 0x1f) + 2) * 2;

                        // stream copy
                        for (int o = 0; o < count; o++)
                        {
                            decompressedData.Add(decompressedData[currentPos - negativeOffset]);
                            currentPos++;
                        }
                    }
                    else
                    {
                        // no command, copy word to output buffer
                        decompressedData.Add(data[readPos]);
                        readPos++;
                        decompressedData.Add(data[readPos]);
                        readPos++;

                        currentPos += 2;
                    }
                    bitfield = bitfield << 1;
                }
            }
        }
    }
}