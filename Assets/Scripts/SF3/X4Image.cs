using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Model;

namespace Shiningforce
{
    public class X4Image
    {
        string _name;

        byte[] _data;

        public Texture2D ReadFile(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                return null;
            }

            _name = Util.FileSystemHelper.GetFileNameWithoutExtensionFromPath(filePath);

            _data = File.ReadAllBytes(filePath);

            List<Color> palette = ReadPalette(0, 512);

            Texture2D groundImage = CreatePalettizedTextureFromMemory(0x200, 512, 256, palette);

            //byte[] bytes = groundImage.EncodeToPNG();
            //File.WriteAllBytes(Directory.GetCurrentDirectory() + "/textures/" + _name + "_x4image.png", bytes);

            return groundImage;
        }

        protected List<Color> ReadPalette(int offset, int size)
        {
            List<Color> palette = new List<Color>();

            int numColors = size / 2;

            for (int count = 0; count < numColors; count++)
            {
                int value = ByteArray.GetInt16(_data, offset);
                Color rgbColor = ColorHelper.Convert(value);
                palette.Add(rgbColor);
                offset += 2;
            }

            return palette;
        }

        Texture2D CreatePalettizedTextureFromMemory(int offset, int width, int height, List<Color> palette)
        {
            Texture2D texture = new Texture2D(width, height);

            palette[0] = new Color(palette[0].r, palette[0].g, palette[0].b, 0f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = palette[_data[offset]];
                    texture.SetPixel(x, y, color);

                    offset++;
                }
            }

            texture.Apply();

            return texture;
        }
    }
}
