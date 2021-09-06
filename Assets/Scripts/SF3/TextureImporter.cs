using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shiningforce
{
    public class TextureImporter
    {
        //public static void ImportTextures(List<Texture2D> textureList)
        //{
        //    int textureOffset = 0;

        //    for (int count = 0; count < textureCount; count++)
        //    {
        //        int width = ByteArray.GetInt16(_data, defOffset);
        //        int height = ByteArray.GetInt16(_data, defOffset + 2);

        //        //Debug.Log("tex " + count + ": " + width + " * " + height);

        //        Texture2D texture = new Texture2D(width, height);
        //        _textures.Add(texture);

        //        for (int y = 0; y < height; y++)
        //        {
        //            for (int x = 0; x < width; x++)
        //            {
        //                int colorValue = ByteArray.GetInt16(decompressedTextures, textureOffset);
        //                Color color = ColorHelper.Convert(colorValue);

        //                texture.SetPixel(x, y, color);

        //                textureOffset += 2;
        //            }
        //        }

        //        texture.Apply();

        //        defOffset += 8;

        //        //byte[] bytes = texture.EncodeToPNG();
        //        //File.WriteAllBytes(Application.streamingAssetsPath + "/textures/" + _name + "_" + count + ".png", bytes);
        //    }
        //}
    }
}
