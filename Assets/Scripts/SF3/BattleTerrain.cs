using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Model;

namespace Shiningforce
{
    public class BattleTerrain
    {
        string _name;

        byte[] _data;

        int _offsetGroundImage;
        int _sizeGroundImage;

        int _offsetTextures;
        int _sizeTextures;

        int _offsetMeshes;
        int _sizeMeshes;

        List<Texture2D> _textures = new List<Texture2D>();

        ModelData _model;
        ModelTexture _modelTexture;

        class ObjectInstance
        {
            public int MeshId;
            public Vector3 EulerAngles;
            public Vector3 Position;
            public Vector3 Scale;
        }

        List<ObjectInstance> _objects = new List<ObjectInstance>();

        public bool ReadFile(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                return false;
            }

            _model = new ModelData();
            _model.Init();

            _modelTexture = new ModelTexture();
            int textureWidth = 512;
            int textureHeight = 512;
            _modelTexture.Init(false, textureWidth, textureHeight);
            //_modelTexture.Texture.filterMode = FilterMode.Point;
            _model.ModelTexture = _modelTexture;

            _name = Util.FileSystemHelper.GetFileNameWithoutExtensionFromPath(filePath);

            _data = File.ReadAllBytes(filePath);

            // read header
            _offsetGroundImage = ByteArray.GetInt32(_data, 0x00);
            _sizeGroundImage = ByteArray.GetInt32(_data, 0x04);

            _offsetMeshes = ByteArray.GetInt32(_data, 0x10);
            _sizeMeshes = ByteArray.GetInt32(_data, 0x14);

            _offsetTextures = ByteArray.GetInt32(_data, 0x20);
            _sizeTextures = ByteArray.GetInt32(_data, 0x24);

            //Debug.Log("GroundImage at: " + _offsetGroundImage.ToString("X6") + " size: " + _sizeGroundImage.ToString("X6"));
            //Debug.Log("Meshes at: " + _offsetMeshes.ToString("X6") + " size: " + _sizeMeshes.ToString("X6"));
            //Debug.Log("Textures at: " + _offsetTextures.ToString("X6") + " size: " + _sizeTextures.ToString("X6"));

            ReadGroundImage();
            ReadTextures();
            ReadMeshes();

            return true;
        }

        private void ReadGroundImage()
        {
            List<Color> palette = ReadPalette(_offsetGroundImage + 4, 512);

            int height = (_sizeGroundImage - 0x200) / 512;
            Texture2D groundImage = CreatePalettizedTextureFromMemory(_offsetGroundImage + 0x204, 512, height, palette);

            //byte[] bytes = groundImage.EncodeToPNG();
            //File.WriteAllBytes(Directory.GetCurrentDirectory() + "/textures/" + _name  + "_groundImage.png", bytes);

            // create model part with single polygon
            ModelPart part = new ModelPart();
            part.Init();
            _model.Parts.Add(part);

            bool transparent = false;
            bool halftransparent = false;
            bool hflip = false;
            bool vflip = false;
            bool doubleSided = false;
            Color rgbColor = Color.white;
            Vector3 faceNormal = new Vector3(0f, 1f, 0f);

            Vector2 uvA, uvB, uvC, uvD;
            uvA = Vector2.zero;
            uvB = Vector2.zero;
            uvC = Vector2.zero;
            uvD = Vector2.zero;

            int animationGroupId = -1;

            // add texture to atlas
            _modelTexture.AddTexture(groundImage, transparent, halftransparent);
            _modelTexture.AddUv(groundImage, hflip, vflip, out uvA, out uvB, out uvC, out uvD); // add texture uv

            const float tileX = 256f;
            const float tileZ = 128.5f;

            Vector3 vA, vB, vC, vD;
            vD = new Vector3(tileX, 0f, tileZ);
            vC = new Vector3(-tileX, 0f, tileZ);
            vB = new Vector3(-tileX, 0f, -tileZ);
            vA = new Vector3(tileX, 0f, -tileZ);

            int subDivide = 1;

            part.AddPolygon(vA, vB, vC, vD,
                            halftransparent, doubleSided,
                            rgbColor, rgbColor, rgbColor, rgbColor,
                            uvA, uvB, uvC, uvD,
                            faceNormal, faceNormal, faceNormal, faceNormal, subDivide, true, animationGroupId);

            _modelTexture.ApplyTexture();
        }

        private void ReadTextures()
        {
            int offset = _offsetTextures;
            int offsetTextures = ByteArray.GetInt32(_data, offset);
            int headerSize = ByteArray.GetInt32(_data, offset + 4);
            int textureCount = ByteArray.GetInt32(_data, offset + 8);
            int decompressedSize = ByteArray.GetInt32(_data, offset + 12);

            //Debug.Log("Offset Textures: " + offsetTextures.ToString("X6"));
            //Debug.Log("Header size: " + headerSize.ToString("X6"));
            //Debug.Log("Decomp-Size header: " + decompressedSize.ToString("X6"));
            //Debug.Log("Texture count: " + textureCount.ToString("X6"));

            int defOffset = offset + 24;

            byte[] decompressedTextures = Decompression.DecompressData(_data, offset + offsetTextures + 8);

            //Debug.Log("Decompressed size: " + decompressedTextures.Length.ToString("X6"));

            int textureOffset = 0;

            for (int count = 0; count < textureCount; count++)
            {
                int width = ByteArray.GetInt16(_data, defOffset);
                int height = ByteArray.GetInt16(_data, defOffset + 2);

                //Debug.Log("tex " + count + ": " + width + " * " + height);

                Texture2D texture = new Texture2D(width, height);
                _textures.Add(texture);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int colorValue = ByteArray.GetInt16(decompressedTextures, textureOffset);
                        Color color = ColorHelper.Convert(colorValue);

                        if (colorValue == 0 || colorValue == 0x7fff)
                        {
                            color = Color.black;
                            color.a = 0f;
                        }

                        texture.SetPixel(x, y, color);

                        textureOffset += 2;
                    }
                }

                texture.Apply();

                defOffset += 8;

                //byte[] bytes = texture.EncodeToPNG();
                //File.WriteAllBytes(Directory.GetCurrentDirectory() + "/textures/" + _name + "_tx_" + count + ".png", bytes);
            }
        }

        private void ReadMeshes()
        {
            int offset = _offsetMeshes;
            int instancesOffset = ByteArray.GetInt32(_data, offset);
            int meshOffset = ByteArray.GetInt32(_data, offset + 4);

            offset = _offsetMeshes + instancesOffset;

            while (true)
            {
                int meshId = ByteArray.GetInt16(_data, offset);
                if (meshId == 0xffff)
                {
                    break;
                }

                offset += 2;

                ObjectInstance instance = new ObjectInstance();

                instance.MeshId = meshId;

                float x = ByteArray.GetInt16(_data, offset) / (65536f / 360f);
                float y = ByteArray.GetInt16(_data, offset + 2) / (65536f / 360f);
                float z = ByteArray.GetInt16(_data, offset + 4) / (65536f / 360f);
                instance.EulerAngles = new Vector3(x, y, z);
                offset += 6;

                instance.Position = ByteArray.GetVector3(_data, offset);
                offset += 12;

                instance.Scale = ByteArray.GetVector3(_data, offset);
                offset += 12;

                //Debug.Log("objmesh: " + instance.MeshId);
                //Debug.Log("angles: " + instance.EulerAngles);
                //Debug.Log("position: " + instance.Position);
                //Debug.Log("scale: " + instance.Scale);

                _objects.Add(instance);
            }

            ImportMeshesFromOffset(_offsetMeshes + meshOffset);
        }

        void ImportMeshesFromOffset(int offset)
        {
            int startOffset = offset;
            //Debug.Log(startOffset.ToString("X6"));

            while (ByteArray.GetInt32(_data, offset) != -1)
            {
                ModelPart part = new ModelPart();
                part.Init();

                int pointsOffset = ByteArray.GetInt32(_data, offset);
                int numPoints = ByteArray.GetInt32(_data, offset + 0x04);
                int polygonOffset = ByteArray.GetInt32(_data, offset + 0x08);
                int numPolygons = ByteArray.GetInt32(_data, offset + 0x0c);
                int polygonAttributesOffset = ByteArray.GetInt32(_data, offset + 0x10);

                //Debug.Log("Points offset: " + pointsOffset.ToString("X6"));
                //Debug.Log("Points num: " + numPoints.ToString("X6"));
                //Debug.Log("Polygon offset: " + polygonOffset.ToString("X6"));
                //Debug.Log("Polygon num: " + numPolygons.ToString("X6"));
                //Debug.Log("Attribute offset: " + polygonAttributesOffset.ToString("X6"));

                List<Vector3> points = new List<Vector3>();
                List<Vector3> normals = new List<Vector3>();

                // read points
                for (int countPoints = 0; countPoints < numPoints; countPoints++)
                {
                    Vector3 point = ByteArray.GetVector3(_data, _offsetMeshes + pointsOffset);
                    points.Add(point);

                    pointsOffset += 12;
                }

                // read polygons
                for (int countPolygons = 0; countPolygons < numPolygons; countPolygons++)
                {
                    // face normal
                    float nX, nY, nZ;
                    nX = ByteArray.GetFloat(_data, _offsetMeshes + polygonOffset);
                    nY = ByteArray.GetFloat(_data, _offsetMeshes + polygonOffset + 4);
                    nZ = ByteArray.GetFloat(_data, _offsetMeshes + polygonOffset + 8);
                    Vector3 faceNormal = new Vector3(nX, nY, nZ);

                    int a, b, c, d;
                    a = ByteArray.GetInt16(_data, _offsetMeshes + polygonOffset + 12);
                    b = ByteArray.GetInt16(_data, _offsetMeshes + polygonOffset + 14);
                    c = ByteArray.GetInt16(_data, _offsetMeshes + polygonOffset + 16);
                    d = ByteArray.GetInt16(_data, _offsetMeshes + polygonOffset + 18);

                    polygonOffset += 20;

                    // attributes
                    int flag_sort = ByteArray.GetInt16(_data, _offsetMeshes + polygonAttributesOffset);
                    int texno = ByteArray.GetInt16(_data, _offsetMeshes + polygonAttributesOffset + 2);
                    int attributes = ByteArray.GetInt16(_data, _offsetMeshes + polygonAttributesOffset + 4);
                    int colno = ByteArray.GetInt16(_data, _offsetMeshes + polygonAttributesOffset + 6);
                    int gouraudTable = ByteArray.GetInt16(_data, _offsetMeshes + polygonAttributesOffset + 8);
                    int dir = ByteArray.GetInt16(_data, _offsetMeshes + polygonAttributesOffset + 10);

                    polygonAttributesOffset += 12;

                    bool doubleSided = false;
                    if ((flag_sort >> 8) == 1)
                    {
                        doubleSided = true;
                    }

                    bool transparent = false;
                    if ((flag_sort & (1 << 8)) != 0)
                    {
                        transparent = true;
                    }

                    bool halftransparent = false;
                    if ((dir & (1 << 7)) != 0)
                    {
                        halftransparent = true;
                    }

                    int polyType = dir & 0x0f;

                    bool hflip = false;
                    bool vflip = false;
                    if ((dir & (1 << 4)) != 0)
                    {
                        hflip = true;
                    }
                    if ((dir & (1 << 5)) != 0)
                    {
                        vflip = true;
                    }

                    Vector2 uvA, uvB, uvC, uvD;
                    uvA = Vector2.zero;
                    uvB = Vector2.zero;
                    uvC = Vector2.zero;
                    uvD = Vector2.zero;

                    Color rgbColor = ColorHelper.Convert(colno);
                    //rgbColor = Color.white;

                    if (polyType == 2)
                    {
                        // textured polygon
                        rgbColor = Color.white;
                        Texture2D texture = _textures[texno];

                        if (_modelTexture.ContainsTexture(texture) == false)
                        {
                            _modelTexture.AddTexture(texture, transparent, halftransparent);
                        }

                        _modelTexture.AddUv(texture, hflip, vflip, out uvA, out uvB, out uvC, out uvD); // add texture uv
                    }
                    else
                    {
                        // no texture, just color => create colored texture
                        //
                        Texture2D colorTex = new Texture2D(2, 2);
                        Color[] colors = new Color[4];
                        colors[0] = rgbColor;
                        colors[1] = rgbColor;
                        colors[2] = rgbColor;
                        colors[3] = rgbColor;
                        colorTex.SetPixels(colors);
                        colorTex.Apply();

                        _modelTexture.AddTexture(colorTex, true, false);
                        _modelTexture.AddUv(colorTex, hflip, vflip, out uvA, out uvB, out uvC, out uvD);

                        rgbColor = Color.white;
                    }

                    Vector3 vA, vB, vC, vD;
                    vA = points[a];
                    vB = points[b];
                    vC = points[c];
                    vD = points[d];

                    int subDivide = 1;

                    part.AddPolygon(vA, vB, vC, vD,
                                    halftransparent, doubleSided,
                                    rgbColor, rgbColor, rgbColor, rgbColor,
                                    uvA, uvB, uvC, uvD,
                                    faceNormal, faceNormal, faceNormal, faceNormal, subDivide);
                }

                _model.Parts.Add(part);

                offset += 0x14;
            }

            _model.ModelTexture.ApplyTexture();
        }

        public GameObject CreateObject(Material opaqueMaterial, Material transparentMaterial, bool noRoot = true)
        {
            GameObject parent = new GameObject(_name);
            GameObject root;

            if (noRoot == false)
            {
                root = new GameObject("root");
                _model.Root = root;
                root.transform.parent = parent.transform;
                root.transform.localPosition = Vector3.zero;
                root.transform.localEulerAngles = Vector3.zero;
            }
            else
            {
                root = parent;
            }

            float brightness = 1f;
            Color albedo = new Color(brightness, brightness, brightness, 1.0f);

            _model.OpaqueMaterial = new Material(opaqueMaterial);
            _model.OpaqueMaterial.SetColor("_Color", albedo);
            _model.OpaqueMaterial.mainTexture = _model.ModelTexture.Texture;
            //modelData.OpaqueMaterial.enableInstancing = true;

            _model.TransparentMaterial = new Material(transparentMaterial);
            _model.TransparentMaterial.SetColor("_Color", albedo);
            _model.TransparentMaterial.mainTexture = _model.ModelTexture.Texture;

            GameObject partObject;

            int parts = _model.Parts.Count;

            for (int partIndex = 0; partIndex < parts; partIndex++)
            {
                ModelPart part = _model.Parts[partIndex];

                Mesh mesh = part.CreateMesh();

                partObject = new GameObject("part" + partIndex);
                partObject.SetActive(true);

                part.OpaqueObject = partObject;
                if (part.Parent == -1)
                {
                    partObject.transform.parent = root.transform;
                }
                else
                {
                    partObject.transform.parent = _model.Parts[part.Parent].OpaqueObject.transform;
                }
                partObject.transform.localPosition = part.Translation;
                partObject.transform.localScale = new Vector3(1f, 1f, 1f);

                if (mesh != null)
                {
                    MeshFilter filter = partObject.AddComponent<MeshFilter>();
                    filter.mesh = mesh;

                    MeshRenderer renderer = partObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = _model.OpaqueMaterial;
                }

                // transparent
                mesh = part.CreateTransparentMesh();

                if (mesh != null)
                {
                    partObject = new GameObject("part_trans_" + partIndex);
                    partObject.SetActive(true);

                    part.TransparentObject = partObject;

                    if (part.Parent == -1)
                    {
                        partObject.transform.parent = root.transform;
                    }
                    else
                    {
                        partObject.transform.parent = _model.Parts[part.Parent].OpaqueObject.transform;
                    }
                    partObject.transform.localPosition = part.Translation;
                    partObject.transform.localScale = new Vector3(1f, 1f, 1f);

                    MeshFilter filter = partObject.AddComponent<MeshFilter>();
                    filter.mesh = mesh;

                    MeshRenderer renderer = partObject.AddComponent<MeshRenderer>();

                    renderer.sharedMaterial = _model.TransparentMaterial;
                }

                if (part.OpaqueObject || part.TransparentObject)
                {
                    // pivoting
                    GameObject partPivot = new GameObject("pivot" + partIndex);

                    if (part.OpaqueObject)
                    {
                        partPivot.transform.position = part.OpaqueObject.transform.position;
                        partPivot.transform.parent = part.OpaqueObject.transform.parent;
                    }
                    else
                    {
                        partPivot.transform.position = part.TransparentObject.transform.position;
                        partPivot.transform.parent = part.TransparentObject.transform.parent;
                    }

                    partPivot.transform.localEulerAngles = Vector3.zero;

                    if (part.OpaqueObject)
                    {
                        part.OpaqueObject.transform.parent = partPivot.transform;
                    }

                    if (part.TransparentObject)
                    {
                        part.TransparentObject.transform.parent = partPivot.transform;
                    }

                    part.Pivot = partPivot;

                    // translations provided?
                    //if (translatonList != null)
                    //{
                    //    partPivot.transform.position = translatonList[partIndex];
                    //}
                }
            }

            parent.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            // dont receive shadows
            //Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
            //foreach (Renderer renderer in renderers)
            //{
            //    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            //}

            GameObject terrainRoot = new GameObject("BattleTerrain");

            GameObject ground = _model.Parts[0].Pivot;
            ground.transform.parent = terrainRoot.transform;
            ground.transform.localScale = new Vector3(-1f, -1f, 1f);

            foreach (ObjectInstance instance in _objects)
            {
                GameObject obj = GameObject.Instantiate(_model.Parts[instance.MeshId + 1].Pivot);
                obj.transform.position = instance.Position;
                obj.transform.eulerAngles = instance.EulerAngles;
                obj.transform.localScale = instance.Scale;

                obj.transform.parent = terrainRoot.transform;
            }

            terrainRoot.transform.localScale = new Vector3(0.1f, -0.1f, 0.1f);

            GameObject terrainRootMirror = GameObject.Instantiate(terrainRoot);
            terrainRootMirror.name = "Mirror";
            terrainRootMirror.transform.position = new Vector3(0f, 0f, -25.6f);
            terrainRootMirror.transform.localScale = new Vector3(-0.1f, -0.1f, -0.1f);
            terrainRootMirror.transform.parent = terrainRoot.transform;

            Vector3 mirrorGroundScale = terrainRootMirror.transform.GetChild(0).transform.localScale;
            mirrorGroundScale.x *= -1f;
            terrainRootMirror.transform.GetChild(0).transform.localScale = mirrorGroundScale;

            parent.SetActive(false);

            return terrainRoot;
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
