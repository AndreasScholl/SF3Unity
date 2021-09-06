using System.Collections.Generic;
using UnityEngine;

namespace Model
{
    public class ModelData
    {
        public struct AnimationParams
        {
            public short Type;
            public short Repeat;
            public short Speed;
        }

        public GameObject Root;
        public List<ModelPart> Parts;
        public ModelTexture ModelTexture;

        public Dictionary<string, List<Vector3>[]> Animations;
        public Dictionary<string, List<Vector3>> Translations;
        public Dictionary<string, AnimationParams> AnimationParameters;

        public Material OpaqueMaterial;
        public Material TransparentMaterial;

        public void Init()
        {
            Parts = new List<ModelPart>();

            Animations = new Dictionary<string, List<Vector3>[]>();
            Translations = new Dictionary<string, List<Vector3>>();
            AnimationParameters = new Dictionary<string, AnimationParams>();
        }

        public Transform GetPartParent(int index)
        {
            return Parts[index].OpaqueObject.transform.parent;
        }

        public Transform GetPart(int index)
        {
            return Parts[index].OpaqueObject.transform;
        }

        public Transform GetPartTransparent(int index)
        {
            return Parts[index].TransparentObject.transform;
        }
    }

    public class ModelTexture
    {
        int _width = 1024;
        int _height = 1024;

        int X = 0;
        int Y = 0;
        int RowHeight = 0;

        public Texture2D Texture;

        public List<Texture2D> AddedTextures;
        public List<Vector4> TextureAreas;

        public void Init(bool transparentBackground = false, int width = 2048, int height = 1024)
        {
            _width = width;
            _height = height;

            Texture = new Texture2D(_width, _height);

            Color fillColor = new Color(1.0f, 1.0f, 1.0f);

            if (transparentBackground == true)
            {
                fillColor.a = 0f;
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    Texture.SetPixel(x, y, fillColor);
                }
            }
            Texture.Apply();

            AddedTextures = new List<Texture2D>();

            TextureAreas = new List<Vector4>();
        }

        public void AddTexture(Texture2D texture, bool transparent = true, bool halftransparent = true)
        {
            int addWidth = texture.width;
            int addHeight = texture.height;

            if ((X + addWidth) > _width)
            {
                X = 0;
                Y += RowHeight;
                RowHeight = 0;
            }

            AddedTextures.Add(texture);

            Vector4 area = new Vector4();
            area.x = X;
            area.y = Y;
            area.z = X + addWidth;
            area.w = Y + addHeight;
            TextureAreas.Add(area);

            for (int yc = 0; yc < addHeight; yc++)
            {
                for (int xc = 0; xc < addWidth; xc++)
                {
                    Color pixel = texture.GetPixel(xc, yc);

                    //if (transparent == false)
                    //{
                    //    pixel.a = 1f;   // remove cutout alpha
                    //}

                    //if (transparent == false)
                    //{
                    //    if (pixel == Color.black || pixel == Color.white)
                    //    {
                    //        pixel.a = 0f;
                    //    }
                    //}

                    if (halftransparent == true)
                    {
                        pixel.a = 0.5f;
                    }

                    Texture.SetPixel(X + xc, Y + yc, pixel);
                }
            }

            X += addWidth;

            if (addHeight > RowHeight)
            {
                RowHeight = addHeight;
            }
        }

        public void ApplyTexture()
        {
            Texture.Apply();
        }

        public int AddTexture(Texture2D texture, bool transparent = true, bool halftransparent = true, byte[] paletteData = null, int paletteOffset = 0, int cutOutColor = 0, bool overWriteCutoutColor = false)
        {
            int addWidth = texture.width;
            int addHeight = texture.height;

            if ((X + addWidth) > _width)
            {
                X = 0;
                Y += RowHeight;
                RowHeight = 0;
            }

            AddedTextures.Add(texture);

            Vector4 area = new Vector4();
            area.x = X;
            area.y = Y;
            area.z = X + addWidth;
            area.w = Y + addHeight;
            TextureAreas.Add(area);

            for (int yc = 0; yc < addHeight; yc++)
            {
                for (int xc = 0; xc < addWidth; xc++)
                {
                    Color pixel = texture.GetPixel(xc, yc);

                    if (paletteData != null)
                    {
                        // get color index from pixel
                        int colorIndex = (int)(pixel.r * 16f);

                        if (colorIndex < 16)
                        {
                            // get color from palette
                            int colorValue = ByteArray.GetInt16(paletteData, paletteOffset + (colorIndex * 2));
                            Color colorRgb = ColorHelper.Convert(colorValue);
                            //pixel = colorRgb.gamma;
                            pixel = colorRgb;

                            if (transparent == true)
                            {
                                if (colorIndex == cutOutColor)
                                {
                                    if (overWriteCutoutColor == true)
                                    {
                                        pixel = Color.black;
                                    }

                                    pixel.a = 0f;
                                }
                            }
                        }
                        else
                        {
                            pixel = Color.magenta;  // error color
                        }
                    }

                    if (transparent == false)
                    {
                        pixel.a = 1f;
                    }

                    if (halftransparent == true)
                    {
                        pixel.a = 0.5f;
                    }

                    Texture.SetPixel(X + xc, Y + yc, pixel);
                }
            }

            X += addWidth;

            if (addHeight > RowHeight)
            {
                RowHeight = addHeight;
            }

            //Texture.Apply();

            return TextureAreas.Count - 1;  // return area index
        }

        public void AddTextureForSheet(Texture2D texture, int gridWidth, int gridHeight)
        {
            int textureWidth = texture.width;
            int textureHeight = texture.height;

            if ((X + gridWidth) > _width)
            {
                X = 0;
                Y -= gridHeight;
            }

            //AddedTextures.Add(texture);

            Vector4 area = new Vector4();
            area.x = X;
            area.y = Y;
            area.z = X + gridWidth;
            area.w = Y + gridHeight;
            //TextureAreas.Add(area);

            int xCenterOffset = (gridWidth - textureWidth) / 2;
            int yCenterOffset = (gridHeight - textureHeight) / 2;

            for (int yc = 0; yc < textureHeight; yc++)
            {
                for (int xc = 0; xc < textureWidth; xc++)
                {
                    Color pixel = texture.GetPixel(xc, yc);
                    Texture.SetPixel(X + xc + xCenterOffset, Y + yc + yCenterOffset, pixel);
                }
            }

            X += gridWidth;

            Texture.Apply();
        }

        public bool ContainsTexture(Texture2D texture)
        {
            if (AddedTextures.Contains(texture))
            {
                return true;
            }

            return false;
        }

        public void AddUv(List<Vector2> uvList, Texture2D texture, bool hflip, bool vflip, int textureIndex = -1)
        {
            if (textureIndex == -1)
            {
                textureIndex = AddedTextures.IndexOf(texture);
            }

            Vector4 area = TextureAreas[textureIndex];

            float halfU = 0.5f / (float)_width;
            float halfV = 0.5f / (float)_height;

            float fullU = 1f / (float)_width;
            float fullV = 1f / (float)_height;

            Vector2 uvA = new Vector2((fullU * area.x) + halfU, (fullV * area.y) + halfV);
            Vector2 uvB = new Vector2((fullU * area.z) - halfU, (fullV * area.y) + halfV);
            Vector2 uvC = new Vector2((fullU * area.z) - halfU, (fullV * area.w) - halfV);
            Vector2 uvD = new Vector2((fullU * area.x) + halfU, (fullV * area.w) - halfV);

            Vector2 uv1, uv2, uv3, uv4;

            if (vflip == false)
            {
                if (hflip == false)
                {
                    uv1 = uvA;
                    uv2 = uvB;
                    uv3 = uvC;
                    uv4 = uvD;
                }
                else
                {
                    uv1 = uvB;
                    uv2 = uvA;
                    uv3 = uvD;
                    uv4 = uvC;
                }
            }
            else
            {
                if (hflip == false)
                {
                    uv1 = uvD;
                    uv2 = uvC;
                    uv3 = uvB;
                    uv4 = uvA;
                }
                else
                {
                    uv1 = uvD;
                    uv2 = uvC;
                    uv3 = uvB;
                    uv4 = uvA;
                }
            }

            uvList.Add(uv1); // a
            uvList.Add(uv2); // b
            uvList.Add(uv3); // c
            uvList.Add(uv4); // d
        }

        public void AddUv(Texture2D texture, bool hflip, bool vflip, out Vector2 uv1, out Vector2 uv2, out Vector2 uv3, out Vector2 uv4, int textureIndex = -1)
        {
            if (textureIndex == -1)
            {
                textureIndex = AddedTextures.IndexOf(texture);
            }

            Vector4 area = TextureAreas[textureIndex];

            float halfU = 0.5f / (float)_width;
            float halfV = 0.5f / (float)_height;

            float fullU = 1f / (float)_width;
            float fullV = 1f / (float)_height;

            Vector2 uvA = new Vector2((fullU * area.x) + halfU, (fullV * area.y) + halfV);
            Vector2 uvB = new Vector2((fullU * area.z) - halfU, (fullV * area.y) + halfV);
            Vector2 uvC = new Vector2((fullU * area.z) - halfU, (fullV * area.w) - halfV);
            Vector2 uvD = new Vector2((fullU * area.x) + halfU, (fullV * area.w) - halfV);

            if (vflip == false)
            {
                if (hflip == false)
                {
                    uv1 = uvA;
                    uv2 = uvB;
                    uv3 = uvC;
                    uv4 = uvD;
                }
                else
                {
                    uv1 = uvB;
                    uv2 = uvA;
                    uv3 = uvD;
                    uv4 = uvC;
                }
            }
            else
            {
                if (hflip == false)
                {
                    uv1 = uvD;
                    uv2 = uvC;
                    uv3 = uvB;
                    uv4 = uvA;
                }
                else
                {
                    uv1 = uvC;
                    uv2 = uvD;
                    uv3 = uvA;
                    uv4 = uvB;
                }
            }
        }

        public void GetSheetUv(Texture2D sheet, int frameWidth, int frameHeight, bool hflip, bool vflip, out Vector2 uv1, out Vector2 uv2, out Vector2 uv3, out Vector2 uv4)
        {
            Vector4 area = new Vector4();
            area.x = 0f;
            area.y = 0f;
            area.z = frameWidth;
            area.w = frameHeight;

            float halfU = 0.5f / (float)sheet.width;
            float halfV = 0.5f / (float)sheet.height;

            float fullU = 1f / (float)sheet.width;
            float fullV = 1f / (float)sheet.height;

            Vector2 uvA = new Vector2((fullU * area.x) + halfU, (fullV * area.y) + halfV);
            Vector2 uvB = new Vector2((fullU * area.z) - halfU, (fullV * area.y) + halfV);
            Vector2 uvC = new Vector2((fullU * area.z) - halfU, (fullV * area.w) - halfV);
            Vector2 uvD = new Vector2((fullU * area.x) + halfU, (fullV * area.w) - halfV);

            if (vflip == false)
            {
                if (hflip == false)
                {
                    uv1 = uvA;
                    uv2 = uvB;
                    uv3 = uvC;
                    uv4 = uvD;
                }
                else
                {
                    uv1 = uvB;
                    uv2 = uvA;
                    uv3 = uvD;
                    uv4 = uvC;
                }
            }
            else
            {
                if (hflip == false)
                {
                    uv1 = uvD;
                    uv2 = uvC;
                    uv3 = uvB;
                    uv4 = uvA;
                }
                else
                {
                    uv1 = uvC;
                    uv2 = uvD;
                    uv3 = uvA;
                    uv4 = uvB;
                }
            }
        }

        public void SetY(int y)
        {
            Y = y;
        }
    }
}
