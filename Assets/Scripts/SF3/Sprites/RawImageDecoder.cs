using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Shiningforce

{
    public static class RawImageDecoder
    {
        public static ushort ReverseColors(ushort inBytes)
        {

            var r = inBytes & 0x1f;
            var g = (inBytes & (0x1f << 5)) >> 5;
            var b = (inBytes & (0x1f << 10)) >> 10;
            var a = (inBytes & 0x8000);// | 0x8000;

            return (ushort)(a | (r << 10) | (g << 5) | b);

        }
        public static byte[] ReverseColors(byte[] bytes, bool isBigEndian = false)
        {
            var output = new List<byte>();
            for (int i = 0; i < bytes.Count(); i += 2)
            {
                var c = (ushort)((bytes[i] << 8) | bytes[i + 1]);
                if (isBigEndian)
                {
                    c = (ushort)((bytes[i + 1] << 8) | bytes[i]);
                }
                var color = ReverseColors(c);

                output.Add((byte)(color));
                output.Add((byte)(color >> 8));
            }
            return output.ToArray();
        }

        public static byte[] DecompressImage(BinaryReader br)//06013762
        {
            List<ushort> output = new List<ushort>();
            uint encodingIdx = 4;
            uint stackIdx = 0;
            uint stackMax = 0x7C;
            var color = (ushort)0x0000;
            long maxPosition = 0;
            var offset = (uint)br.Position();
            var flag = 0x80000000u;
            //Allocate 256 bytes of space
            var stack = new ushort[0x80];
            stack[stackMax + 1] = 0x0000;
            stack[stackMax + 2] = 0x8000;
            stack[stackMax + 3] = 0x7FFF;

            var flagStart = (uint)br.ReadUInt32().FlipEndian();
           
            var flagIdx = flagStart;
            uint nextFlagSet()
            {
                br.Position(offset + flagIdx);
                flagIdx += 2;
                maxPosition = Math.Max(maxPosition, br.Position());
                return (uint)((br.ReadUInt16().FlipEndian()) << 16) | 0x8000;
            }
     
            br.Position(offset + flagIdx);
            var read = br.ReadByte();
            if ((read & 1) != 0)
            {
                flag = (uint)(read << 24) | 0x800000;
                flagIdx++;
            }
            
            maxPosition = Math.Max(maxPosition, br.Position());


            while (encodingIdx < flagStart)
            {
                br.Position(offset + encodingIdx++);
                var low = (uint)(sbyte)br.ReadByte();
                maxPosition = Math.Max(maxPosition, br.Position());

                if ((int)low >= 0) color = (stack[low]);
                else
                {
                    low <<= 8;
                    br.Position(offset + encodingIdx++);
                    color = (ushort)(br.ReadByte()|low);
                    maxPosition = Math.Max(maxPosition, br.Position());

                    stack[stackIdx++] = color;
                    if (stackIdx > stackMax) stackIdx = 0;
                }

                if (flag == 0x80000000) flag = nextFlagSet();

                if ((flag >> 31) == 1)
                {
                    flag <<= 1;
                    var branch = 1;

                    while ((flag >> 31) == 1)
                    {
                        flag <<= 1;
                        if (flag != 0) branch++;
                        else flag = nextFlagSet();
                    }
                    flag <<= 1;

                    var leaf = 1;

                    while (branch != 0)
                    {
                        leaf = (leaf << 1) | (int)(flag >> 31);
                        flag <<= 1;
                        if (flag != 0) branch--;
                        else
                        {
                            flag = nextFlagSet();
                            leaf >>= 1;
                        }
                    }

                    while (leaf-- > 0) output.Add(color);

                }
                else
                {
                    output.Add(color);
                    flag <<= 1;
                }

            }

            br.Position(maxPosition);
            if (br.Position() < br.Length() && (maxPosition & 0x7FF) == 0)
            {
                br.Position((br.Position() - 4) & 0xFFFFFFC);
                while (br.ReadUInt32() != 0) ;
            }

            return ReverseColors(output.ToByteArray());
        }

        

    }
}
