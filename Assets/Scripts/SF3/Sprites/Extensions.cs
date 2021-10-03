using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Shiningforce
{
    public static class Extensions
    {
        public static short FlipEndian(this short value) => (short)FlipEndian((ushort)value);
        public static ushort FlipEndian(this ushort value) => (ushort)(((value & 0xFF00) >> 8) | ((value & 0xFF) << 8));
        public static int FlipEndian(this int value) => (int)FlipEndian((uint)value);
        public static uint FlipEndian(this uint value) => ((value & 0xFF0000) >> 8) | ((value & 0xFF00) << 8) | ((value & 0xFF000000) >> 24) | ((value & 0xFF) << 24);
        public static void Position(this BinaryReader br, long value) => br.BaseStream.Position = value;
        public static long Position(this BinaryReader br) => br.BaseStream.Position;
        public static long Length(this BinaryReader br) => br.BaseStream.Length;

        public static Texture2D ToTexture2D(this byte[] data, int width, int height)
        {
            var buffer = data;
            var pixels = new ushort[buffer.Length / 2];
            Buffer.BlockCopy(buffer, 0, pixels, 0, buffer.Length);
            var texture = new Texture2D(width, height);
            var x = 0;
            var y = 0;
            foreach (var pixel in pixels)
            {
                texture.SetPixel(x++, y, pixel.ToColor());
                if (x < width) continue;
                x = 0;
                y++;
            }
            return texture;
        }

        public static Color ToColor(this ushort color)
        {

            var r = (color >> 10) & 0x1F;
            var g = (color >> 05) & 0x1F;
            var b = (color >> 00) & 0x1F;

            var rPerc = r / 31f;
            var gPerc = g / 31f;
            var bPerc = b / 31f;

            var red = (byte)((r << 3) | (byte)(0x7 * rPerc));
            var green = (byte)((g << 3) | (byte)(0x7 * gPerc));
            var blue = (byte)((b << 3) | (byte)(0x7 * bPerc));

            return new Color(red / 255f, green / 255f, blue / 255f, (color & 0x8000) >> 15);
        }
        public static void Add(this List<byte> data, int value) => data.Add((uint)value);

        public static void Add(this List<byte> data, uint value)
        {
            data.Add((byte)(value >> 24));
            data.Add((byte)(value >> 16));
            data.Add((byte)(value >> 8));
            data.Add((byte)value);
        }
        public static void Add(this List<byte> data, short value) => data.Add((ushort)value);

        public static void Add(this List<byte> data, ushort value)
        {
            data.Add((byte)(value >> 8));
            data.Add((byte)value);
        }

        public static byte[] ToByteArray(this short[] buffer) => buffer.SelectMany(b => new[] { (byte)(b >> 8), (byte)b }).ToArray();

        public static byte[] ToByteArray(this ushort[] buffer) => buffer.SelectMany(b => new[] { (byte)(b >> 8), (byte)b }).ToArray();

        public static byte[] ToByteArray(this List<short> buffer) => buffer.ToArray().ToByteArray();
        public static byte[] ToByteArray(this List<ushort> buffer) => buffer.ToArray().ToByteArray();


    }
}
