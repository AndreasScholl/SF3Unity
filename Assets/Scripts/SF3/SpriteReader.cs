using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SpriteReader
{
    class ColorStream
    {
        byte[] _data;
        int _windowPosition;
        int _streamPosition;
        List<int> _colorWindow;
        bool _first = true;

        public ColorStream(byte[] colorData)
        {
            _data = colorData;

            // rotating color window has a size of 0x80, as only 7 bits are used as index
            _colorWindow = new List<int>(Enumerable.Repeat(0, 0x80));
            // fill default values.
            _colorWindow[0x7f] = 0x7fff;
            _colorWindow[0x7e] = 0x8000;
            _colorWindow[0x7d] = 0;

            _windowPosition = 0;
            _streamPosition = 0;
        }

        public bool hasMore()
        {
            return _streamPosition < _data.Length;
        }

        public int nextColor()
        {
            int colorByte = _data[_streamPosition];
            _streamPosition++;

            // index into rotating color window?
            if (colorByte >= 0 && colorByte < 0x80)
            {
                // yes, return the color
                return _colorWindow[colorByte];
            }

            // no. read second color byte and
            int color = (((colorByte & 0xff) << 8) | _data[_streamPosition]) & 0xffff;
            _streamPosition++;

            // add color to color window
            _colorWindow[_windowPosition] = color;

            // increase window position, rotating bacek to start on overflow. Default values are not part of the
            // rotating window.
            _windowPosition = (_windowPosition + 1) % 0x7d;
            return color;
        }
    }

    private class BitStream
    {
        int _currentByte;
        int _remainingBits;

        byte[] _data;
        int _offset;

        public BitStream(byte[] data, int offset)
        {
            _data = data;
            _offset = offset;

            _remainingBits = 0;
        }

        public int nextBit()
        {
            if (_remainingBits == 0)
            {
                _currentByte = _data[_offset];
                _offset++;
                _remainingBits = 8;
            }

            // extract msb
            int bit = (_currentByte & 0x80) >> 7;

            // move msb out
            _currentByte <<= 1;
            // reduce number of remaining bits in the current byte
            _remainingBits--;
            return bit;
        }

        public int GetOffset()
        {
            return _offset;
        }
    }

    public static int ReadSprite(byte[] data, int offset, int width, int height, string name, ref Color[] sprite)
    {
        int colorDataSize = ByteArray.GetInt32(data, offset);
        offset += 4;

        // copy color data
        byte[] colorData = new byte[colorDataSize - 4];
        for (int count = 0; count < colorData.Length; count++)
        {
            colorData[count] = data[offset + count];
        }

        offset = offset + colorData.Length;
        List<int> colors = DecompressImage(colorData, data, ref offset);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index < colors.Count)
                {
                    int colorValue = colors[index];
                    Color color = ColorHelper.Convert(colorValue);
                    if (colorValue == 0 || colorValue == 0x7fff)
                    {
                        color = Color.black;
                        color.a = 0f;
                    }
                    sprite[index] = color;
                }
            }
        }

        return offset;
    }

    public static List<int> DecompressImage(byte[] colorData, byte[] data, ref int offset)
    {
        List<int> colors = new List<int>();

        // prepare command bitstream
        BitStream commandBitStream = new BitStream(data, offset);
        ColorStream colorStream = new ColorStream(colorData);

        while (colorStream.hasMore())
        {
            int color = colorStream.nextColor();

            // copy single?
            if (commandBitStream.nextBit() == 0)
            {
                // copy single and continue
                colors.Add(color);
                continue;
            }

            int bitLength = 1;

            // count number of '1', consuming the first '0' in the process.
            // Note: the bitLength is at least 1, when the copy bit is followed directly by a zero '0'
            while (commandBitStream.nextBit() != 0)
            {
                bitLength++;
            }

            // msb is always 1. As the bitLength os at least 1 too, the minimal count is
            // 10 binary or 2 decimal when the only count bit is '0'.
            int count = 1;

            // read number of bits for repeat count.
            for (int i = 0; i < bitLength; i++)
            {
                count <<= 1;
                if (commandBitStream.nextBit() == 1)
                {
                    // add the read bit as lsb
                    count += 1;
                }
            }

            // Output color
            for (int i = 0; i < count; i++)
            {
                colors.Add(color);
            }
        }

        offset = commandBitStream.GetOffset();

        return colors;
    }
}
