using Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Shiningforce
{
    public class ChpData
    {
        string _name;

        byte[] _data;

        int _sheetCount = 0;

        Texture2D[] _textures;
        List<SheetInfo> _sheetInfos = new List<SheetInfo>();

        class SheetInfo
        {
            public int SpriteWidth;
            public int SpriteHeight;
            public int SpriteCount;
            public int Rows;
            public int Columns;
        }

        public bool ReadFile(string filePath, int columns = 6)
        {
            if (File.Exists(filePath) == false)
            {
                return false;
            }

            // extract name from path
            _name = Util.FileSystemHelper.GetFileNameWithoutExtensionFromPath(filePath);
            Debug.Log("Map: " + _name);

            _data = File.ReadAllBytes(filePath);

            List<Texture2D> textures = new List<Texture2D>();

            int sheetOffset = 0;
            bool endOfSprites = false;

            while (endOfSprites == false)
            {
                Texture2D texture = ReadSpriteSheet(ref sheetOffset, columns);

                if (texture == null)
                {
                    endOfSprites = true;
                }
                else
                {
                    // skip to next full 0x800 boundary
                    sheetOffset = ((sheetOffset + 0x7ff) / 0x800) * 0x800;

                    if (sheetOffset >= _data.Length)
                    {
                        endOfSprites = true;
                    }

                    textures.Add(texture);

                    _sheetCount++;
                }
            }

            _textures = textures.ToArray();

            return true;
        }

        Texture2D ReadSpriteSheet(ref int sheetOffset, int columns)
        {
            int offset = sheetOffset;

            int characterSheetNo = ByteArray.GetInt16(_data, offset);
            if (characterSheetNo == 0xffff)
            {
                return null;
            }

            int width = ByteArray.GetInt16(_data, offset + 2);
            int height = ByteArray.GetInt16(_data, offset + 4);

            int unknown1 = ByteArray.GetInt16(_data, offset + 6);
            int unknown2 = ByteArray.GetInt16(_data, offset + 8);
            int unknown3 = ByteArray.GetInt16(_data, offset + 10);
            int unknown4 = ByteArray.GetInt16(_data, offset + 12);
            int unknown5 = ByteArray.GetInt16(_data, offset + 14);

            int spriteListOffset = ByteArray.GetInt32(_data, offset + 16) + sheetOffset;
            int animationsOffset = ByteArray.GetInt32(_data, offset + 20) + sheetOffset;

            int endOfList = ByteArray.GetInt16(_data, offset + 24);
            //offset += 26;

            // read list of animation offsets. Always 0x10 entries, unused entries are zero
            List<int> animationOffsets = new List<int>();
            offset = animationsOffset;
            for (int i = 0; i < 0x10; i++)
            {
                int animOffset = ByteArray.GetInt32(_data, offset) + sheetOffset;
                if (animOffset != 0)
                {
                    animationOffsets.Add(animOffset);
                }
                offset += 4;
            }

            // read animations
            for (int count = 0; count < animationOffsets.Count; count++)
            {
                int animOffset = animationOffsets[count];

                Debug.Log(animOffset.ToString("X6"));
                for (int i = 0; i < 0x10; i++)
                {
                    int spriteIndex = ByteArray.GetInt16(_data, animOffset);
                    int attribute = ByteArray.GetInt16(_data, animOffset + 2);

                    Debug.Log("spriteIndex: " + spriteIndex + " attrib: " + attribute.ToString("X4"));

                    if (attribute == 0)
                    {
                        // maybe: if spriteIndex msb is set?
                        break;
                    }

                    animOffset += 4;
                }
            }

            List<int> offsets = new List<int>();

            offset = spriteListOffset;

            while (true)
            {
                int spriteOffset = ByteArray.GetInt32(_data, offset);

                if (spriteOffset == 0)
                {
                    break;
                }

                offsets.Add(spriteOffset + sheetOffset);

                offset += 4;
            }

            List<Color[]> sprites = new List<Color[]>();

            foreach (int spriteOffset in offsets)
            {
                Color[] sprite = new Color[width * height];

                //Debug.Log("sprite offset: " + spriteOffset.ToString("X6"));
                sheetOffset = SpriteReader.ReadSprite(_data, spriteOffset, width, height, _name, ref sprite);

                sprites.Add(sprite);
            }

            // create sprite sheet texture
            int sheetWidth = width * columns;
            int rows = sprites.Count / columns;
            if ((sprites.Count % columns) != 0)
            {
                rows++;
            }
            int sheetHeight = rows * height;
            Texture2D texture = new Texture2D(sheetWidth, sheetHeight);

            for (int sY = 0; sY < rows; sY++)
            {
                for (int sX = 0; sX < columns; sX++)
                {
                    int spriteIndex = (sY * columns) + sX;

                    if (spriteIndex >= sprites.Count)
                    {
                        break;
                    }

                    Color[] sprite = sprites[spriteIndex];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Color pixel = sprite[(y * width) + x];
                            texture.SetPixel((sX * width) + x, (sheetHeight - 1) - ((sY * height) + y), pixel);
                        }
                    }
                }
            }

            texture.Apply();

            //byte[] bytes = texture.EncodeToPNG();
            //File.WriteAllBytes(Directory.GetCurrentDirectory() + "/textures/" + _name + "_sheet" + _sheetCount + ".png", bytes);

            SheetInfo info = new SheetInfo();
            info.SpriteWidth = width;
            info.SpriteHeight = height;
            info.Rows = rows;
            info.Columns = columns;
            info.SpriteCount = sprites.Count;
            _sheetInfos.Add(info);

            return texture;
        }

        public Sprite[] CreateSprites(int sheetIndex)
        {
            List<Sprite> sprites = new List<Sprite>();

            Texture2D texture = _textures[sheetIndex];

            SheetInfo info = _sheetInfos[sheetIndex];

            int index = 0;
            for (int sY = info.Rows - 1; sY >= 0; sY--) // note: start with bottom row
            {
                for (int sX = 0; sX < info.Columns; sX++)
                {
                    if (index >= info.SpriteCount)
                    {
                        break;
                    }

                    Sprite sprite = Sprite.Create(texture, new Rect(info.SpriteWidth * sX,
                                                                    info.SpriteHeight * sY,
                                                                    info.SpriteWidth, info.SpriteHeight), new Vector2(0.5f, 0.5f), 100.0f);

                    sprites.Add(sprite);

                    index++;
                }
            }

            return sprites.ToArray();
        }
    }
}