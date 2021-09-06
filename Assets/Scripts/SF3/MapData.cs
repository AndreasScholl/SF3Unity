using Model;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Shiningforce
{
    public class MapData : MonoBehaviour
    {
        string _name;

        MemoryManager _memory;

        const int MEMORY_MPDBASE = 0x290000;

        const int MEMORY_POINTER_TABLE_OFFSET = 0x2000;
        const int MEMORY_MAPOBJECTS_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (1 * 8);
        const int MEMORY_SURFACEDATA_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (2 * 8);
        const int MEMORY_TEXTURE_ANIMATION_DATA_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (3 * 8);
        //const int MEMORY_UNKNOWNEMPTY_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (4 * 8);
        const int MEMORY_SURFACE2_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (5 * 8);
        const int MEMORY_TEXTUREGROUPS_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (6 * 8);

        const int MEMORY_MAPOBJECT_DATA = MEMORY_MPDBASE + 0x2100;

        int _mapObjectOffset;
        //int _mapObjectSize;
        int _surfaceDataPointer;
        int _textureAnimationDataPointer;

        List<int> _textureGroupOffsets = new List<int>();

        ModelData _model;
        ModelTexture _modelTexture;

        List<MapObjectHeader> _mapObjects = new List<MapObjectHeader>();
        List<Texture2D> _mapTextures = new List<Texture2D>();

        int _textureAnimationsMemory;
        List<TextureAnimation> _textureAnimations = new List<TextureAnimation>();

        class MapObjectHeader
        {
            public List<int> Pointers = new List<int>();

            public Vector3 Position;
            public Vector3 EulerAngles;
            public Vector3 Scale;
        }

        class TextureAnimation
        {
            public int Group;
            public int Width;
            public int Height;
            public int Speed;
            public List<TextureFrame> Frames = new List<TextureFrame>();
            public Texture2D Texture;
            public Material Material;

            public float Time = 0f;
            public int CurrentFrame = 0;
            public float TimePerFrame = 0.2f;
        }

        class TextureFrame
        {
            public int Offset;
            public int Unknown;
            public Texture2D Texture;
        }

        public bool ReadFile(string file)
        {
            if (File.Exists(file) == false)
            {
                return false;
            }

            // extract name from path
            int start = file.LastIndexOf('/');
            int end = file.IndexOf('.');
            _name = file.Substring(start, end - start);

            _model = new ModelData();
            _model.Init();

            _modelTexture = new ModelTexture();
            int textureWidth = 512;
            int textureHeight = 512;
            _modelTexture.Init(false, textureWidth, textureHeight);
            _model.ModelTexture = _modelTexture;

            //_data = File.ReadAllBytes(file);
            _memory = new MemoryManager(0x200000);
            _memory.LoadFile(file, MEMORY_MPDBASE);

            ReadHeaderInfo();
            ReadElementOffsets();

            ReadMapTextures();
            ReadMapTextureAnimations();
            ReadMapObjects();

            return true;
        }

        void ReadElementOffsets()
        {
            //chunk.addBlock(readOffsetBlock(stream, "unknown_empty[0]"));

            _mapObjectOffset = _memory.GetInt32(MEMORY_MAPOBJECTS_POINTER);
            //_mapObjectSize = _memory.GetInt32(MEMORY_MAPOBJECT_OFFSET + 4);

            _surfaceDataPointer = _memory.GetInt32(MEMORY_SURFACEDATA_POINTER);
            _textureAnimationDataPointer = _memory.GetInt32(MEMORY_TEXTURE_ANIMATION_DATA_POINTER);

            Debug.Log(_mapObjectOffset.ToString("X6"));
            Debug.Log(_surfaceDataPointer.ToString("X6"));
            Debug.Log(_textureAnimationDataPointer.ToString("X6"));

            // 8 texture group offsets
            for (int count = 0; count < 8; count++)
            {
                int textureGroup = _memory.GetInt32(MEMORY_TEXTUREGROUPS_POINTER + (count * 8));
                _textureGroupOffsets.Add(textureGroup);

                Debug.Log("texture group: " + textureGroup.ToString("X6"));
            }
        }

        void ReadMapObjects()
        {
            int mapPointer = _mapObjectOffset;

            //block.addProperty("header_pointer1", new Pointer(stream, relativeOffset));
            //block.addProperty("header_pointer2", new Pointer(stream, relativeOffset));
            //block.addProperty("numObjects", new HexValue(stream.readShort()));
            //block.addProperty("header_zero", new HexValue(stream.readShort()));

            int numObjects = _memory.GetInt16(_mapObjectOffset + 0x08);

            Debug.Log("num objects: " + numObjects.ToString("X4"));

            int readPointer = _mapObjectOffset + 0x0c;

            for (int objCount = 0; objCount < numObjects; objCount++)
            {
                MapObjectHeader header = new MapObjectHeader();

                for (int count = 0; count < 8; count++)
                {
                    int pointer = _memory.GetInt32(readPointer);
                    header.Pointers.Add(pointer);
                    readPointer += 4;
                }

                float x, y, z;
                x = (short)_memory.GetInt16(readPointer);
                y = (short)_memory.GetInt16(readPointer + 2);
                z = (short)_memory.GetInt16(readPointer + 4);
                header.Position = new Vector3(x, y, z);
                readPointer += 6;

                x = _memory.GetInt16(readPointer) / (65536f / 360f);
                y = _memory.GetInt16(readPointer + 2) / (65536f / 360f);
                z = _memory.GetInt16(readPointer + 4) / (65536f / 360f);
                header.EulerAngles = new Vector3(x, y, z);
                readPointer += 6;

                x = _memory.GetFloat(readPointer);
                y = _memory.GetFloat(readPointer + 4);
                z = _memory.GetFloat(readPointer + 8);
                header.Scale = new Vector3(x, y, z);
                readPointer += 12;

                // padding
                readPointer += 4;

                _mapObjects.Add(header);

                //Debug.Log("objCount: " + objCount + "rot: " + header.EulerAngles);
                //Debug.Log("pos: " + header.Position);
                //Debug.Log("scl: " + header.Scale);

                int xpDataPointer = CorrectMapObjectPointer(header.Pointers[0]);
                ImportMeshFromOffset(xpDataPointer);

                //foreach (int pointer in header.Pointers)
                //{
                //    int xpDataPointer = CorrectMapObjectPointer(pointer);

                //    Debug.Log("xpdata: " + xpDataPointer.ToString("X6"));

                //    ImportMeshFromOffset(xpDataPointer);
                //}
            }

            _model.ModelTexture.ApplyTexture();
        }

        private int CorrectMapObjectPointer(int pointer)
        {
            if (pointer >= 0x60a0000)
            {
                return (pointer - 0x60a0000) + MEMORY_MAPOBJECT_DATA;
            }

            return pointer;
        }

        void ImportMeshFromOffset(int offset)
        {
            ModelPart part = new ModelPart();
            part.Init();

            int pointsOffset = CorrectMapObjectPointer(_memory.GetInt32(offset));
            int numPoints = _memory.GetInt32(offset + 0x04);
            int polygonOffset = CorrectMapObjectPointer(_memory.GetInt32(offset + 0x08));
            int numPolygons = _memory.GetInt32(offset + 0x0c);
            int polygonAttributesOffset = CorrectMapObjectPointer(_memory.GetInt32(offset + 0x10));

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
                Vector3 point = _memory.GetVector3(pointsOffset);
                points.Add(point);

                pointsOffset += 12;
            }

            // read polygons
            for (int countPolygons = 0; countPolygons < numPolygons; countPolygons++)
            {
                // face normal
                float nX, nY, nZ;
                nX = _memory.GetFloat(pointsOffset);
                nY = _memory.GetFloat(pointsOffset + 4);
                nZ = _memory.GetFloat(pointsOffset + 8);
                Vector3 faceNormal = new Vector3(nX, nY, nZ);
                //Debug.Log(faceNormal);

                int a, b, c, d;
                a = _memory.GetInt16(polygonOffset + 12);
                b = _memory.GetInt16(polygonOffset + 14);
                c = _memory.GetInt16(polygonOffset + 16);
                d = _memory.GetInt16(polygonOffset + 18);

                polygonOffset += 20;

                // attributes
                int flag_sort = _memory.GetInt16(polygonAttributesOffset);
                int texno = _memory.GetInt16(polygonAttributesOffset + 2);
                int attributes = _memory.GetInt16(polygonAttributesOffset + 4);
                int colno = _memory.GetInt16(polygonAttributesOffset + 6);
                int gouraudTable = _memory.GetInt16(polygonAttributesOffset + 8);
                int dir = _memory.GetInt16(polygonAttributesOffset + 10);

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

                TextureAnimation textureAnimation = GetTextureAnimationByGroupId(texno);
                int animationGroupId = -1;

                if (polyType == 2)
                {
                    // textured polygon
                    rgbColor = Color.white;

                    if (textureAnimation == null)
                    {
                        // add texture to atlas
                        Texture2D texture = _mapTextures[texno];

                        if (_modelTexture.ContainsTexture(texture) == false)
                        {
                            _modelTexture.AddTexture(texture, transparent, halftransparent);
                        }

                        _modelTexture.AddUv(texture, hflip, vflip, out uvA, out uvB, out uvC, out uvD); // add texture uv
                    }
                    else
                    {
                        // get uv based on texture animation sheet
                        _modelTexture.GetSheetUv(textureAnimation.Texture, textureAnimation.Width, textureAnimation.Height, 
                                                 hflip, vflip, out uvA, out uvB, out uvC, out uvD);

                        animationGroupId = textureAnimation.Group;
                    }
                }

                Vector3 vA, vB, vC, vD;
                vA = points[a];
                vB = points[b];
                vC = points[c];
                vD = points[d];

                int subDivide = 2;

                part.AddPolygon(vA, vB, vC, vD,
                                halftransparent, doubleSided,
                                rgbColor, rgbColor, rgbColor, rgbColor,
                                uvA, uvB, uvC, uvD,
                                faceNormal, faceNormal, faceNormal, faceNormal, subDivide, true, animationGroupId);
            }

            _model.Parts.Add(part);
        }

        public GameObject CreateObject(Material opaqueMaterial, Material transparentMaterial, bool noRoot = true)
        {
            GameObject parent = new GameObject("mapObjects");
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

            // create animated materials
            foreach (TextureAnimation animation in _textureAnimations)
            {
                Material material = new Material(opaqueMaterial);
                material.SetColor("_Color", albedo);
                material.mainTexture = animation.Texture;
                animation.Material = material;
            }

            GameObject partObject;

            int parts = _model.Parts.Count;

            bool recalculateNormals = false;

            for (int partIndex = 0; partIndex < parts; partIndex++)
            {
                ModelPart part = _model.Parts[partIndex];

                Mesh mesh = part.CreateMesh();

                if (recalculateNormals)
                {
                    if (mesh != null)
                    {
                        mesh.RecalculateNormals();
                    }
                }

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

                // animated meshes
                Dictionary<int, Mesh> animatedMeshes = part.CreateAnimatedMeshes();

                foreach (KeyValuePair<int, Mesh> pair in animatedMeshes)
                {
                    partObject = new GameObject("part_anim_" + partIndex);
                    partObject.SetActive(true);

                    part.AnimatedObjects.Add(partObject);

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
                    filter.mesh = pair.Value;

                    MeshRenderer renderer = partObject.AddComponent<MeshRenderer>();
                    TextureAnimation animation = GetTextureAnimationByGroupId(pair.Key);
                    renderer.sharedMaterial = animation.Material;
                }

                // transparent
                mesh = part.CreateTransparentMesh();

                if (mesh != null)
                {
                    if (recalculateNormals)
                    {
                        mesh.RecalculateNormals();
                    }

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

                    foreach (GameObject animatedObject in part.AnimatedObjects)
                    {
                        animatedObject.transform.parent = partPivot.transform;
                    }

                    part.Pivot = partPivot;

                    MapObjectHeader objectHeader = _mapObjects[partIndex];
                    partPivot.transform.localPosition = objectHeader.Position;
                    partPivot.transform.localScale = objectHeader.Scale;

                    Quaternion rotation = Quaternion.AngleAxis(objectHeader.EulerAngles.z, Vector3.forward) *
                                          Quaternion.AngleAxis(objectHeader.EulerAngles.y, Vector3.up) *
                                          Quaternion.AngleAxis(objectHeader.EulerAngles.x, Vector3.right);
                    partPivot.transform.rotation = rotation;
                }
            }

            parent.transform.localScale = new Vector3(0.1f, -0.1f, 0.1f);

            // dont receive shadows
            //Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
            //foreach (Renderer renderer in renderers)
            //{
            //    renderer.receiveShadows = false;
            //}

            return parent;
        }

        private void ReadMapTextures()
        {
            for (int count = 0; count < _textureGroupOffsets.Count; count++)
            {
                if (count < 5)  // first 5 groups are map object textures
                {
                    int textureMemory = _textureGroupOffsets[count];

                    byte[] decompressedTextures = Decompression.DecompressData(_memory.Data, textureMemory - _memory.Base);

                    int numTextures = ByteArray.GetInt16(decompressedTextures, 0);
                    int textureIdStart = ByteArray.GetInt16(decompressedTextures, 2);

                    Debug.Log("num textures: " + numTextures);
                    Debug.Log("textureIdStart: " + textureIdStart);

                    int defOffset = 4;
                    for (int textureCount = 0; textureCount < numTextures; textureCount++)
                    {
                        int width = decompressedTextures[defOffset];
                        int height = decompressedTextures[defOffset + 1];
                        int textureOffset = ByteArray.GetInt16(decompressedTextures, defOffset + 2);
                        defOffset += 4;

                        //Debug.Log("w: " + width + " h: " + height + " offs: " + textureOffset.ToString("X6"));

                        Texture2D texture = CreateTextureFromMemory(decompressedTextures, textureOffset, width, height);
                        _mapTextures.Add(texture);

                        //byte[] bytes = texture.EncodeToPNG();
                        //File.WriteAllBytes(Application.streamingAssetsPath + "/textures/" + _name + "_" + (_mapTextures.Count - 1) + ".png", bytes);
                    }
                }
            }
        }

        private void ReadMapTextureAnimations()
        {
            // read animation definitions
            int readPointer = _textureAnimationsMemory;

            while (true)
            {
                int groupIndex = _memory.GetInt16(readPointer);
                if (groupIndex == 0xffff)
                {
                    break;
                }

                TextureAnimation animation = new TextureAnimation();
                animation.Group = groupIndex;
                animation.Width = _memory.GetInt16(readPointer + 2);
                animation.Height = _memory.GetInt16(readPointer + 4);
                animation.Speed = _memory.GetInt16(readPointer + 6);
                readPointer += 8;

                Debug.Log("txanim group: " + groupIndex);
                Debug.Log("txanim width: " + animation.Width);
                Debug.Log("txanim height: " + animation.Height);
                Debug.Log("txanim speed: " + animation.Speed);

                while (true)
                {
                    int offset = _memory.GetInt16(readPointer);
                    readPointer += 2;
                    if (offset == 0xfffe)
                    {
                        // no more frames in this animation
                        break;
                    }

                    TextureFrame frame = new TextureFrame();
                    frame.Offset = offset;
                    _memory.GetInt16(readPointer + 2);
                    readPointer += 2;

                    animation.Frames.Add(frame);

                    //Debug.Log("frame offset: " + offset.ToString("X4"));
                }

                _textureAnimations.Add(animation);
            }

            // read animation frame textures
            foreach (TextureAnimation textureAnimation in _textureAnimations)
            {
                for (int count = 0; count < textureAnimation.Frames.Count; count++)
                {
                    int textureAddress = _textureAnimationDataPointer + textureAnimation.Frames[count].Offset;

                    byte[] decompressedTexture = Decompression.DecompressData(_memory.Data, textureAddress - _memory.Base);
                    Texture2D texture = CreateTextureFromMemory(decompressedTexture, 0, textureAnimation.Width, textureAnimation.Height);

                    textureAnimation.Frames[count].Texture = texture;

                    //byte[] bytes = texture.EncodeToPNG();
                    //File.WriteAllBytes(Application.streamingAssetsPath + "/textures/" + _name + "_anim_" + textureAnimation.Group + "_frame" + count + ".png", bytes);
                }

                // create texture sheet
                Texture2D textureSheet = CreateTextureSheet(textureAnimation);
                textureAnimation.Texture = textureSheet;

                //byte[] bytes = textureSheet.EncodeToPNG();
                //File.WriteAllBytes(Application.streamingAssetsPath + "/textures/" + _name + "_anim_" + textureAnimation.Group + ".png", bytes);
            }
        }

        private void ReadHeaderInfo()
        {
            int headerStart = _memory.GetInt32(MEMORY_MPDBASE);
            int headerPointer = _memory.GetInt32(headerStart);

            int value = _memory.GetInt32(headerPointer);
            int offset1 = _memory.GetInt32(headerPointer + 0x04);
            int offset2 = _memory.GetInt32(headerPointer + 0x08);
            int offset3 = _memory.GetInt32(headerPointer + 0x0c);
            int value2 = _memory.GetInt32(headerPointer + 0x10);
            int offset4 = _memory.GetInt32(headerPointer + 0x14);
            _textureAnimationsMemory = _memory.GetInt32(headerPointer + 0x18);

            Debug.Log("texture animations: " + _textureAnimationsMemory.ToString("X6"));
        }

        Texture2D CreateTextureFromMemory(byte[] data, int offset, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int colorValue = ByteArray.GetInt16(data, offset);
                    Color color = ColorHelper.Convert(colorValue);

                    if (colorValue == 0 || colorValue == 0x7fff)
                    {
                        color = Color.black;
                        color.a = 0f;
                    }

                    texture.SetPixel(x, y, color);

                    offset += 2;
                }
            }

            texture.Apply();

            return texture;
        }

        private Texture2D CreateTextureSheet(TextureAnimation animation)
        {
            int frameCount = animation.Frames.Count;

            int gridWidth = animation.Width;
            int gridHeight = animation.Height;

            int sheetWidth = gridWidth * frameCount;
            int sheetHeight = gridHeight;

            ModelTexture modelTexture = new ModelTexture();
            modelTexture.Init(true, sheetWidth, sheetHeight);
            modelTexture.SetY(sheetHeight - gridHeight);

            // copy frame to sheet
            for (int count = 0; count < frameCount; count++)
            {
                modelTexture.AddTextureForSheet(animation.Frames[count].Texture, gridWidth, gridHeight);
            }

            return modelTexture.Texture;
        }

        private bool IsAnimatedTexture(int texno)
        {
            if (GetTextureAnimationByGroupId(texno) != null)
            {
                return true;
            }

            return false;
        }

        private TextureAnimation GetTextureAnimationByGroupId(int texno)
        {
            foreach (TextureAnimation animation in _textureAnimations)
            {
                if (animation.Group == texno)
                {
                    return animation;
                }
            }

            return null;
        }

        private int GetAnimationGroupIndex(int texno)
        {
            int index = 0;

            foreach (TextureAnimation animation in _textureAnimations)
            {
                if (animation.Group == texno)
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        private void Update()
        {
            for (int count = 0; count < _textureAnimations.Count; count++)
            {
                TextureAnimation animation = _textureAnimations[count];

                int frames = animation.Frames.Count;

                animation.Time += Time.deltaTime;

                if (animation.Time >= animation.TimePerFrame)
                {
                    animation.Time -= animation.TimePerFrame;

                    animation.CurrentFrame++;

                    if (animation.CurrentFrame >= frames)
                    {
                        animation.CurrentFrame = 0;
                    }
                }

                float offset = (1.0f / (float)frames) * animation.CurrentFrame;
                animation.Material.SetTextureOffset("_MainTex", new Vector2(offset, 0f));
            }
        }
    }
}