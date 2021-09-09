﻿using Model;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
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
        const int MEMORY_SURFACE_DATA_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (2 * 8);
        const int MEMORY_TEXTURE_ANIMATION_DATA_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (3 * 8);
        //const int MEMORY_UNKNOWNEMPTY_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (4 * 8);
        const int MEMORY_SURFACE_HEIGHTS_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (5 * 8);
        const int MEMORY_TEXTUREGROUPS_POINTER = MEMORY_MPDBASE + MEMORY_POINTER_TABLE_OFFSET + (6 * 8);

        const int MEMORY_MAPOBJECT_DATA = MEMORY_MPDBASE + 0x2100;

        int _mapObjectOffset;
        //int _mapObjectSize;
        int _surfaceDataPointer;
        int _surfaceDataSize;
        int _surfaceHeightsPointer;
        int _textureAnimationDataPointer;

        List<int> _textureGroupOffsets = new List<int>();
        List<int> _planeOffsets = new List<int>();

        ModelData _model;
        ModelTexture _modelTexture;

        int _surfacePartIndex = -1;

        List<MapObjectHeader> _mapObjects = new List<MapObjectHeader>();
        List<Texture2D> _mapTextures = new List<Texture2D>();

        int _textureAnimationsMemory;
        List<TextureAnimation> _textureAnimations = new List<TextureAnimation>();

        GameObject _debugTxtPrefab = null;

        int _offsetPalette1;
        int _offsetPalette2;
        int _sizePalette1;
        int _sizePalette2;

        class MapObjectHeader
        {
            public List<int> Pointers = new List<int>();

            public Vector3 Position;
            public Vector3 EulerAngles;
            public Vector3 Scale;

            public int Attribute;
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

        public bool ReadFile(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                return false;
            }

            // extract name from path
            _name = Util.FileSystemHelper.GetFileNameWithoutExtensionFromPath(filePath);
            Debug.Log("Map: " + _name);

            // prepare model and texture for map object
            _model = new ModelData();
            _model.Init();

            _modelTexture = new ModelTexture();
            int textureWidth = 512;
            int textureHeight = 512;
            _modelTexture.Init(false, textureWidth, textureHeight);
            _model.ModelTexture = _modelTexture;

            _memory = new MemoryManager(0x200000);
            _memory.LoadFile(filePath, MEMORY_MPDBASE);

            ReadHeaderInfo();
            ReadElementOffsets();

            ReadMapTextures();
            ReadMapTextureAnimations();
            ReadMapObjects();
            ReadSurfaceTiles();

            return true;
        }

        int GetNormalIndexByBlockAndInner(int blockX, int blockY, int innerX, int innerY)
        {
            int normalBlockIndex = (((blockY * 16) + blockX) * 25);
            int normalIndex = normalBlockIndex + (innerY * 5) + innerX;

            return normalIndex;
        }

        void ReadSurfaceTiles()
        {
            if (_surfaceDataSize == 0)
            {
                Debug.Log("No surface tiles on this map!");
                return;
            }

            // create modelpart for surface
            ModelPart part = new ModelPart();
            part.Init();
            _surfacePartIndex = _model.Parts.Count;
            _model.Parts.Add(part);

            byte[] heightData = Decompression.DecompressData(_memory.Data, _surfaceHeightsPointer - _memory.Base);
            //Debug.Log("heightData size: " + heightData.Length);

            int readPointer = _surfaceDataPointer;
            int sizeTileData = 64 * 64 * 2;
            int normalReadPointer = _surfaceDataPointer + sizeTileData;
            int sizeNormalData = 16 * 16 * 25 * 6;
            int byteStructureReadPointer = normalReadPointer + sizeNormalData;

            // 16x16 blocks a 5x5 tiles or structures
            List<Vector3> vertexNormals = new List<Vector3>();
            
            for (int blockY = 0; blockY < 16; blockY++)
            {
                for (int blockX = 0; blockX < 16; blockX++)
                {
                    for (int tileY = 0; tileY < 5; tileY++)
                    {
                        for (int tileX = 0; tileX < 5; tileX++)
                        {
                            //short a = (short)_memory.GetInt16(normalReadPointer);
                            //short b = (short)_memory.GetInt16(normalReadPointer + 2);
                            //short c = (short)_memory.GetInt16(normalReadPointer + 4);
                            //float x = a / (float)0x100;
                            //float y = b / (float)0x100;
                            //float z = c / (float)0x100;

                            float x = _memory.GetFloat16(normalReadPointer);
                            float y = _memory.GetFloat16(normalReadPointer + 2);
                            float z = _memory.GetFloat16(normalReadPointer + 4);

                            //Debug.Log(a.ToString("X4") + " " + b.ToString("X4") + " " + c.ToString("X4"));
                            //Debug.Log(a + " " + b + " " + c);
                            //Debug.Log(x + " " + y + " " + z);
                            Vector3 normal = new Vector3(x, y, z);
                            vertexNormals.Add(normal);
                            normalReadPointer += 6;

                            // unknown byte value
                            int value = _memory.GetByte(byteStructureReadPointer);
                            byteStructureReadPointer++;
                            //Debug.Log(value.ToString("X2"));
                        }
                    }
                }
            }

            // 16x16 blocks a 4x4 tiles
            for (int blockY = 0; blockY < 16; blockY++)
            {
                for (int blockX = 0; blockX < 16; blockX++)
                {
                    for (int innerY = 0; innerY < 4; innerY++)
                    {
                        for (int innerX = 0; innerX < 4; innerX++)
                        {
                            int tileX = blockX * 4 + innerX;
                            int tileY = blockY * 4 + innerY;

                            int heightDataOffset = ((tileY * 64) + tileX) * 4;
                            int heightA = (0x100 - heightData[heightDataOffset + 1]) * 2;
                            int heightB = (0x100 - heightData[heightDataOffset]) * 2;
                            int heightC = (0x100 - heightData[heightDataOffset + 3]) * 2;
                            int heightD = (0x100 - heightData[heightDataOffset + 2]) * 2;
                            //Debug.Log(heightA.ToString("X2") + " " + heightB.ToString("X2") + " " + 
                            //          heightC.ToString("X2") + " " + heightD.ToString("X2"));

                            int normalIndex1 = GetNormalIndexByBlockAndInner(blockX, blockY, innerX, innerY);
                            int normalIndex2 = GetNormalIndexByBlockAndInner(blockX, blockY, innerX + 1, innerY);
                            int normalIndex3 = GetNormalIndexByBlockAndInner(blockX, blockY, innerX + 1, innerY + 1);
                            int normalIndex4 = GetNormalIndexByBlockAndInner(blockX, blockY, innerX, innerY + 1);

                            Vector3 nA = vertexNormals[normalIndex1];
                            Vector3 nB = vertexNormals[normalIndex2];
                            Vector3 nC = vertexNormals[normalIndex3];
                            Vector3 nD = vertexNormals[normalIndex4];

                            int tileData = _memory.GetInt16(readPointer);
                            //Debug.Log(tileData.ToString("X4"));

                            readPointer += 2;

                            int attribute = tileData & 0xff00;

                            //if (attribute != 00)
                            //{
                            //    Debug.Log("attr: " + attribute.ToString("X2"));
                            //}

                            int tileTexture = tileData & 0xff;

                            if (tileTexture != 0xff)
                            {
                                bool transparent = false;
                                bool halftransparent = false;
                                bool hflip = (attribute & 0x1000) != 0;
                                bool vflip = (attribute & 0x2000) == 0;
                                bool doubleSided = false;
                                Color rgbColor = Color.white;
                                Vector3 faceNormal = new Vector3(0f, 1f, 0f);

                                Vector2 uvA, uvB, uvC, uvD;
                                uvA = Vector2.zero;
                                uvB = Vector2.zero;
                                uvC = Vector2.zero;
                                uvD = Vector2.zero;

                                TextureAnimation textureAnimation = GetTextureAnimationByGroupId(tileTexture);
                                int animationGroupId = -1;

                                // textured polygon
                                rgbColor = Color.white;

                                if (textureAnimation == null)
                                {
                                    // add texture to atlas
                                    Texture2D texture = _mapTextures[tileTexture];

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

                                //// add texture to atlas
                                //Texture2D texture = _mapTextures[tileTexture];

                                //if (_modelTexture.ContainsTexture(texture) == false)
                                //{
                                //    _modelTexture.AddTexture(texture, transparent, halftransparent);
                                //}

                                //_modelTexture.AddUv(texture, hflip, vflip, out uvA, out uvB, out uvC, out uvD); // add texture uv

                                const float tileSize = 32f;

                                Vector3 vA, vB, vC, vD;
                                vA = new Vector3(((blockX * 4) + innerX) * tileSize, 0f, ((blockY * 4) + innerY) * tileSize);
                                vA.y = heightA;

                                vB = vA;
                                vB.y = heightB;
                                vB.x += tileSize;

                                vC = vB;
                                vC.y = heightC;
                                vC.z += tileSize;

                                vD = vA;
                                vD.y = heightD;
                                vD.z += tileSize;

                                int subDivide = 2;

                                part.AddPolygon(vA, vB, vC, vD,
                                                halftransparent, doubleSided,
                                                rgbColor, rgbColor, rgbColor, rgbColor,
                                                uvA, uvB, uvC, uvD,
                                                nA, nB, nC, nD, subDivide, true, animationGroupId);
                            }
                        }
                    }
                }
            }

            _modelTexture.ApplyTexture();
        }

        void ReadElementOffsets()
        {
            //chunk.addBlock(readOffsetBlock(stream, "unknown_empty[0]"));

            _mapObjectOffset = _memory.GetInt32(MEMORY_MAPOBJECTS_POINTER);
            //_mapObjectSize = _memory.GetInt32(MEMORY_MAPOBJECT_OFFSET + 4);

            _surfaceDataPointer = _memory.GetInt32(MEMORY_SURFACE_DATA_POINTER);
            _surfaceDataSize = _memory.GetInt32(MEMORY_SURFACE_DATA_POINTER + 4);
            _surfaceHeightsPointer = _memory.GetInt32(MEMORY_SURFACE_HEIGHTS_POINTER);
            _textureAnimationDataPointer = _memory.GetInt32(MEMORY_TEXTURE_ANIMATION_DATA_POINTER);

            //Debug.Log(_mapObjectOffset.ToString("X6"));
            //Debug.Log(_surfaceDataPointer.ToString("X6"));
            //Debug.Log(_surfaceHeightsPointer.ToString("X6"));
            //Debug.Log(_textureAnimationDataPointer.ToString("X6"));

            // 8 texture group offsets
            for (int count = 0; count < 8; count++)
            {
                int textureGroup = _memory.GetInt32(MEMORY_TEXTUREGROUPS_POINTER + (count * 8));
                _textureGroupOffsets.Add(textureGroup);

                //Debug.Log("texture group: " + textureGroup.ToString("X6"));
            }

            // planes
            bool readPlanes = true;
            int readPointer = MEMORY_TEXTUREGROUPS_POINTER + (8 * 8);
            while (readPlanes)
            {
                int planeOffset = _memory.GetInt32(readPointer);
                if (planeOffset == 0)
                {
                    readPlanes = false;
                }
                else
                {
                    _planeOffsets.Add(planeOffset);
                    Debug.Log("plane offfset: " + planeOffset.ToString("X6"));

                    readPointer += 8;
                }
            }
        }

        void ReadMapObjects()
        {
            int mapPointer = _mapObjectOffset;

            int pointer1 = CorrectMapObjectPointer(_memory.GetInt32(_mapObjectOffset));
            int pointer2 = CorrectMapObjectPointer(_memory.GetInt32(_mapObjectOffset + 4));
            //Debug.Log("pointer1: " + pointer1.ToString("X6"));
            //Debug.Log("pointer2: " + pointer2.ToString("X6"));

            int numObjects = _memory.GetInt16(_mapObjectOffset + 0x08);

            //Debug.Log("num objects: " + numObjects.ToString("X4"));

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

                int attribute = _memory.GetInt32(readPointer);
                header.Attribute = attribute;
                //Debug.Log("attribute " + attribute.ToString("X8"));
                readPointer += 4;

                _mapObjects.Add(header);

                //Debug.Log("objCount: " + objCount + "rot: " + header.EulerAngles);
                //Debug.Log("pos: " + header.Position);
                //Debug.Log("scl: " + header.Scale);

                int xpDataPointer = CorrectMapObjectPointer(header.Pointers[0]);
                //Debug.Log("xpData: " + xpDataPointer.ToString("X6"));
                ImportMeshFromOffset(xpDataPointer);
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
                nX = _memory.GetFloat(polygonOffset);
                nY = _memory.GetFloat(polygonOffset + 4);
                nZ = _memory.GetFloat(polygonOffset  + 8);
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
                rgbColor = Color.white;

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

                int subDivide = 1;

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

                    if (partIndex < _mapObjects.Count)
                    {
                        MapObjectHeader objectHeader = _mapObjects[partIndex];
                        partPivot.transform.localPosition = objectHeader.Position;
                        partPivot.transform.localScale = objectHeader.Scale;

                        Quaternion rotation = Quaternion.AngleAxis(objectHeader.EulerAngles.z, Vector3.forward) *
                                              Quaternion.AngleAxis(objectHeader.EulerAngles.y, Vector3.up) *
                                              Quaternion.AngleAxis(objectHeader.EulerAngles.x, Vector3.right);
                        partPivot.transform.rotation = rotation;

                        if ((objectHeader.Attribute & 0x000000008) != 0)
                        {
                            partPivot.AddComponent<RotateTowardsCamera>();
                        }

                        if (objectHeader.Attribute != 0)
                        {
                            CreateDebugText(partPivot, partPivot.transform.position, objectHeader.Attribute.ToString("X8"));
                        }
                    }
                    else if (partIndex == _surfacePartIndex)
                    {
                        partPivot.transform.localPosition = new Vector3(0f, -512f, 0f);
                        partPivot.transform.localScale = new Vector3(-1f, 1f, -1f);
                    }
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

                    //Debug.Log("num textures: " + numTextures);
                    //Debug.Log("textureIdStart: " + textureIdStart);

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

                //Debug.Log("txanim group: " + groupIndex);
                //Debug.Log("txanim width: " + animation.Width);
                //Debug.Log("txanim height: " + animation.Height);
                //Debug.Log("txanim speed: " + animation.Speed);

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

            int offset6 = _memory.GetInt32(headerPointer + 0x1c);
            int offset7 = _memory.GetInt32(headerPointer + 0x20);
            int offsetObject1 = _memory.GetInt32(headerPointer + 0x24);
            int offsetObject2 = _memory.GetInt32(headerPointer + 0x28);
            int offsetObject3 = _memory.GetInt32(headerPointer + 0x2c);
            int value3 = _memory.GetInt32(headerPointer + 0x30);
            int value4 = _memory.GetInt32(headerPointer + 0x34);
            int textureAnimAlternativeOffset = _memory.GetInt32(headerPointer + 0x38);

            _offsetPalette1 = _memory.GetInt32(headerPointer + 0x3c);
            _offsetPalette2 = _memory.GetInt32(headerPointer + 0x40);
            _sizePalette1 = _offsetPalette2 - _offsetPalette1;
            _sizePalette2 = headerPointer - _offsetPalette2;
            Debug.Log("pal1: " + _offsetPalette1.ToString("X6") + " size: " + _sizePalette1.ToString("X6"));
            Debug.Log("pal2: " + _offsetPalette2.ToString("X6") + " size: " + _sizePalette2.ToString("X6"));

            int scrollPlaneX = _memory.GetInt16(headerPointer + 0x44);
            int scrollPlaneY = _memory.GetInt16(headerPointer + 0x46);
            int scrollPlaneZ = _memory.GetInt16(headerPointer + 0x48);
            int unknownAngle = _memory.GetInt16(headerPointer + 0x4a);

            int value5 = _memory.GetInt32(headerPointer + 0x4c);
            int value6 = _memory.GetInt32(headerPointer + 0x50);
            int value7 = _memory.GetInt32(headerPointer + 0x54);

            //Debug.Log("texture animations: " + _textureAnimationsMemory.ToString("X6"));
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

        public Transform CreateDebugText(GameObject root, Vector3 position, string text)
        {
            if (_debugTxtPrefab == null)
            {
                // load ui prefab
                _debugTxtPrefab = Resources.Load<GameObject>("Prefabs/DebugText");
            }

            GameObject textObject = GameObject.Instantiate<GameObject>(_debugTxtPrefab);
            textObject.transform.SetParent(root.transform);

            TextMeshPro textMesh = textObject.GetComponentInChildren<TextMeshPro>();
            textMesh.text = text;

            position.y += -50f;
            textObject.transform.position = position;
            textObject.transform.localScale = new Vector3(5f, -5f, 5f);

            return textObject.transform;
        }

        private void ReadScrollPlanes()
        {
            List<Color> palette1 = ReadPalette(_offsetPalette1, _sizePalette1);
            List<Color> palette2 = ReadPalette(_offsetPalette2, _sizePalette2);

        }

        protected List<Color> ReadPalette(int readPointer, int size)
        {
            List<Color> palette = new List<Color>();

            if (_memory.IsAddressValid(readPointer) == false)
            {
                return palette;
            }

            int numColors = size / 2;

            for (int count = 0; count < numColors; count++)
            {
                int value = _memory.GetInt16(readPointer);
                Color rgbColor = ColorHelper.Convert(value);
                palette.Add(rgbColor);
                readPointer += 2;
            }

            return palette;
        }
    }
}