using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Model;

namespace Shiningforce
{
    public class BattleMesh
    {
        string _name;

        byte[] _data;

        int _offsetTextureDef;
        int _sizeTextureDef;

        int _offsetTextures;
        int _sizeTextures;

        int _offsetMeshes;
        int _sizeMeshes;

        int _offsetAnimations;
        int _sizeAnimations;

        List<Texture2D> _textures = new List<Texture2D>();

        List<HierarchyNode> _hierarchy = new List<HierarchyNode>();

        List<BoneAnimation> _boneAnimList = new List<BoneAnimation>();

        ModelData _model;
        ModelTexture _modelTexture;

        List<AnimationDef> _animDefs = new List<AnimationDef>();

        class BoneAnimation
        {
            public List<int> TranslationTimes;
            public List<int> RotationTimes;
            public List<int> ScaleTimes;

            public List<float> TranslationX;
            public List<float> TranslationY;
            public List<float> TranslationZ;

            public List<float> RotationX;
            public List<float> RotationY;
            public List<float> RotationZ;
            public List<float> RotationW;

            public List<float> ScaleX;
            public List<float> ScaleY;
            public List<float> ScaleZ;
        }

        class SubNode
        {
            public int Id;
            public Vector3 Translation;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        class HierarchyNode
        {
            public int ParentIndex = -1;
            public List<int> PartList = new List<int>();
            public GameObject NodeObject = null;
            public List<SubNode> SubNodes = new List<SubNode>();

            public void AddSubNode(int id, Vector3 translation, Quaternion rotation, Vector3 scale)
            {
                SubNode subNode = new SubNode();
                subNode.Id = id;
                subNode.Translation = translation;
                subNode.Rotation = rotation;
                subNode.Scale = scale;
                SubNodes.Add(subNode);
            }
        }

        class AnimationDef
        {
            public int StartFrame;
            public int Frames;
            public int Type;
        }

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
            _model.ModelTexture = _modelTexture;

            //Debug.Log(pltOffset.ToString("X6"));

            _name = Util.FileSystemHelper.GetFileNameWithoutExtensionFromPath(filePath);

            _data = File.ReadAllBytes(filePath);

            // read header
            _offsetTextureDef = ByteArray.GetInt32(_data, 0x00);

            if (_offsetTextureDef == 0x64756D6D) // dummy?
            {
                return false;
            }

            _sizeTextureDef = ByteArray.GetInt32(_data, 0x04);

            _offsetTextures = ByteArray.GetInt32(_data, 0x10);
            _sizeTextures = ByteArray.GetInt32(_data, 0x14);

            _offsetMeshes = ByteArray.GetInt32(_data, 0x20);
            _sizeMeshes = ByteArray.GetInt32(_data, 0x24);

            _offsetAnimations = ByteArray.GetInt32(_data, 0x30);
            _sizeAnimations = ByteArray.GetInt32(_data, 0x34);

            //Debug.Log("TextureDef at: " + _offsetTextureDef.ToString("X6") + " size: " + _sizeTextureDef.ToString("X6"));
            //Debug.Log("Textures at: " + _offsetTextures.ToString("X6") + " size: " + _sizeTextures.ToString("X6"));
            //Debug.Log("Meshes at: " + _offsetMeshes.ToString("X6") + " size: " + _sizeMeshes.ToString("X6"));
            //Debug.Log("Animations at: " + _offsetAnimations.ToString("X6") + " size: " + _sizeAnimations.ToString("X6"));

            ReadTextures();
            ReadMeshes();
            ReadAnimations();

            return true;
        }

        private void ReadTextures()
        {
            int headerSize = ByteArray.GetInt32(_data, _offsetTextureDef);
            int textureCount = ByteArray.GetInt32(_data, _offsetTextureDef + 0x04);

            //Debug.Log("Texture count: " + textureCount.ToString("X6"));

            int defOffset = _offsetTextureDef + headerSize;

            byte[] decompressedTextures = Decompression.DecompressData(_data, _offsetTextures);

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
                //File.WriteAllBytes(Application.streamingAssetsPath + "/textures/" + _name + "_" + count + ".png", bytes);
            }
        }

        private void ReadMeshes()
        {
            //Debug.Log("Meshes");

            int headerSize = ByteArray.GetInt32(_data, _offsetMeshes);
            int skeletonOffset = ByteArray.GetInt32(_data, _offsetMeshes + 0x04);
            int meshOffset = ByteArray.GetInt32(_data, _offsetMeshes + 0x08);
            int weaponMeshOffset = ByteArray.GetInt32(_data, _offsetMeshes + 0x0c);

            Debug.Log("Header size: " + headerSize.ToString("X6"));
            Debug.Log("skeletonOffset: " + skeletonOffset.ToString("X6"));
            Debug.Log("meshOffset: " + meshOffset.ToString("X6"));
            Debug.Log("weaponMeshOffset: " + weaponMeshOffset.ToString("X6"));

            // hierarchy
            ReadHierarchy(_offsetMeshes + skeletonOffset, _sizeMeshes - skeletonOffset);

            // character meshes
            ImportMeshesFromOffset(_offsetMeshes + meshOffset);

            // weapon meshes
            if (weaponMeshOffset != -1)
            {
                ImportMeshesFromOffset(_offsetMeshes + weaponMeshOffset);
            }
        }

        void ImportMeshesFromOffset(int offset)
        {
            while (ByteArray.GetInt32(_data, offset) != -1)
            {
                ModelPart part = new ModelPart();
                part.Init();

                int pointsOffset = ByteArray.GetInt32(_data, offset);
                int numPoints = ByteArray.GetInt32(_data, offset + 0x04);
                int polygonOffset = ByteArray.GetInt32(_data, offset + 0x08);
                int numPolygons = ByteArray.GetInt32(_data, offset + 0x0c);
                int polygonAttributesOffset = ByteArray.GetInt32(_data, offset + 0x10);
                int normalOffset = ByteArray.GetInt32(_data, offset + 0x14);

                //Debug.Log("Points offset: " + pointsOffset.ToString("X6"));
                //Debug.Log("Points num: " + numPoints.ToString("X6"));
                //Debug.Log("Polygon offset: " + polygonOffset.ToString("X6"));
                //Debug.Log("Polygon num: " + numPolygons.ToString("X6"));
                //Debug.Log("Attribute offset: " + polygonAttributesOffset.ToString("X6"));
                //Debug.Log("Normal offset: " + normalOffset.ToString("X6"));

                List<Vector3> points = new List<Vector3>();
                List<Vector3> normals = new List<Vector3>();

                // read points
                for (int countPoints = 0; countPoints < numPoints; countPoints++)
                {
                    Vector3 point = ByteArray.GetVector3(_data, _offsetMeshes + pointsOffset);
                    points.Add(point);

                    pointsOffset += 12;
                }

                int normalPtr = _offsetMeshes + normalOffset;
                //Debug.Log("Normals at: " + normalPtr.ToString("X6"));

                // read normals
                for (int countPoints = 0; countPoints < numPoints; countPoints++)
                {
                    Vector3 normal = ByteArray.GetVector3_16(_data, _offsetMeshes + normalOffset);
                    normals.Add(normal);

                    normalOffset += 6;

                    //Debug.Log(countPoints + ": " + normal.x + " | " + normal.y + " | " + normal.z);
                }

                // read polygons
                for (int countPolygons = 0; countPolygons < numPolygons; countPolygons++)
                {
                    // face normal
                    //float x, y, z;
                    //x = ByteArray.GetFloat(_data, pointsOffset);
                    //y = ByteArray.GetFloat(_data, pointsOffset + 4);
                    //z = ByteArray.GetFloat(_data, pointsOffset + 8);

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
                    Vector3 nA, nB, nC, nD;
                    nA = normals[a];
                    nB = normals[b];
                    nC = normals[c];
                    nD = normals[d];

                    vA = points[a];
                    vB = points[b];
                    vC = points[c];
                    vD = points[d];

                    int subDivide = 2;

                    part.AddPolygon(vA, vB, vC, vD,
                                    halftransparent, doubleSided,
                                    rgbColor, rgbColor, rgbColor, rgbColor,
                                    uvA, uvB, uvC, uvD,
                                    nA, nB, nC, nD, subDivide);
                }

                _model.Parts.Add(part);

                offset += 0x18;
            }

            _model.ModelTexture.ApplyTexture();
        }

        void ReadHierarchy(int offset, int size)
        {
            //Debug.Log("Hierarchy offset: " + offset.ToString("X6"));
            //Debug.Log("Hierarchy size: " + size.ToString("X6"));

            int partIndex = 0;

            List<int> nodeStack = new List<int>();

            for (int count = 0; count < size; count++)
            {
                int command = _data[offset];
                offset++;

                if (command == 0xfd)
                {
                    InsertNodeIntoHierarchy(nodeStack);

                    //DebugOutputNodeStack(nodeStack);
                }
                else if (command == 0xfe)
                {
                    // pop hierarchy level
                    if (nodeStack.Count > 0)
                    {
                        nodeStack.RemoveAt(nodeStack.Count - 1);
                    }
                    else
                    {
                        break;
                    }

                    //DebugOutputNodeStack(nodeStack);
                }
                else if (command == 0x00 || command == 0x80)
                {
                    // part -> store in hierarchy
                    int nodeIndex = nodeStack[nodeStack.Count - 1];
                    _hierarchy[nodeIndex].PartList.Add(partIndex);

                    //Debug.Log("Part " + partIndex + " parent = " + nodeIndex);

                    partIndex++;
                }
                else if (command < 0x30)
                {
                    offset = GetAligned4Offset(offset);

                    Vector3 translation = ByteArray.GetVector3(_data, offset);
                    offset += 12;
                }
                else if (command == 0x30)
                {
                    offset = GetAligned4Offset(offset);

                    Vector3 translation = ByteArray.GetVector3(_data, offset);
                    offset += 12;

                    Quaternion rotation = ByteArray.GetQuaternion(_data, offset);
                    offset += 16;

                    Vector3 scale = ByteArray.GetVector3(_data, offset);
                    offset += 12;

                    int nodeIndex = nodeStack[nodeStack.Count - 1];
                    _hierarchy[nodeIndex].AddSubNode(command, translation, rotation, scale);
                }
            }
        }

        void DebugOutputNodeStack(List<int> nodeStack)
        {
            string debugStack = "";
            foreach (int idx in nodeStack)
            {
                debugStack += idx.ToString("D2") + " ";
            }
            Debug.Log(debugStack);
        }

        void InsertNodeIntoHierarchy(List<int> nodeStack)
        {
            HierarchyNode node = new HierarchyNode();
            if (nodeStack.Count > 0)
            {
                node.ParentIndex = nodeStack[nodeStack.Count - 1];
            }

            //Debug.Log("node" + _hierarchy.Count + " parent: node" + node.ParentIndex);

            _hierarchy.Add(node);

            nodeStack.Add(_hierarchy.Count - 1);
        }

        int GetAligned4Offset(int offset)
        {
            if ((offset & 3) != 0)
            {
                offset += 4 - (offset & 3);
            }

            return offset;
        }

        private void ReadAnimations()
        {
            Debug.Log("Animations");

            int offset = _offsetAnimations;
            
            Vector3 boundingBoxMin = ByteArray.GetVector3(_data, offset);
            offset += 12;
            Vector3 boundingBoxMax = ByteArray.GetVector3(_data, offset);
            offset += 12;

            //Debug.Log(boundingBoxMin);
            //Debug.Log(boundingBoxMax);

            int boneKeyFramesOffset = ByteArray.GetInt32(_data, offset);
            offset += 4;

            while (true)
            {
                int startFrame = ByteArray.GetInt16(_data, offset);
                offset += 2;

                if (startFrame == 0xffff)
                {
                    break;
                }

                int numberOfFrames = ByteArray.GetInt16(_data, offset);
                offset += 2;

                int type = ByteArray.GetInt16(_data, offset);
                offset += 2;

                int distanceFromEnemy = ByteArray.GetInt16(_data, offset);
                offset += 2;

                int animOffset = ByteArray.GetInt32(_data, offset);
                offset += 4;

                //Debug.Log("StartFrame: " + startFrame.ToString("X6"));
                //Debug.Log("Frames: " + numberOfFrames.ToString("X6"));
                //Debug.Log("Type: " + type.ToString("X6"));
                //Debug.Log("Distance: " + distanceFromEnemy.ToString("X6"));
                //Debug.Log("Offset: " + animOffset.ToString("X6"));
                //Debug.Log("--------------------");

                AnimationDef animDef = new AnimationDef();
                animDef.StartFrame = startFrame & 0xfff;
                animDef.Frames = numberOfFrames;
                animDef.Type = type;
                _animDefs.Add(animDef);
            }

            //    // read animation events
            //    for (Object property : animations.getProperties().values()) 
            //    {
            //        Block block = (Block)property;
            //        int offset = block.getInt("offset");
            //        stream.seek((long) chunk.getStart() + offset);
            //        j = 0;
            //        Block events = block.createBlock("events", chunk.getStart() + offset, 0);
            //        while (true) 
            //        {
            //            int frame = stream.readUnsignedShort();
            //            if (frame == 0xffff) 
            //            {
            //                break;
            //            }
            //            Block event = events.createBlock("event["+j+"]",chunk.getStart() + offset, 0);
            //            event.addProperty("frame", new HexValue(frame));
            //            event.addProperty("eventCode", new HexValue(stream.readUnsignedShort()));
            //            j++;
            //        }
            //    }
            //    header.addProperty("_animation_events_end", new HexValue((int) stream.getStreamPosition()));

            int boneNo = 0;
            offset = _offsetAnimations + boneKeyFramesOffset;
            while (true)
            {
                //Debug.Log("bonekey: " + boneNo);

                BoneAnimation boneAnim = ReadBoneAnim(boneNo, ref offset);
                if (boneAnim == null)
                {
                    break;
                }
                _boneAnimList.Add(boneAnim);
                boneNo++;
            }
        }

        private BoneAnimation ReadBoneAnim(int boneNo, ref int offset)
        {
            BoneAnimation boneAnim = new BoneAnimation();

            int translationKeyCount = ByteArray.GetInt32(_data, offset);
            offset += 4;
            if (translationKeyCount == -1)
            {
                return null;
            }

            int rotationKeyCount = ByteArray.GetInt32(_data, offset);
            offset += 4;
            int scaleKeyCount = ByteArray.GetInt32(_data, offset);
            offset += 4;

            //Debug.Log("TransKeys: " + translationKeyCount.ToString("X6"));
            //Debug.Log("RotatKeys: " + rotationKeyCount.ToString("X6"));
            //Debug.Log("ScaleKeys: " + scaleKeyCount.ToString("X6"));

            // read list offsets
            List<int> offsets = new List<int>();
            for (int o = 0; o < 13; o++)
            {
                int listOffset = ByteArray.GetInt32(_data, offset) + _offsetAnimations;
                offsets.Add(listOffset);
                offset += 4;

                //Debug.Log("ListOffset: " + listOffset.ToString("X6"));
            }

            boneAnim.TranslationTimes = ByteArray.ReadInt16List(_data, translationKeyCount, offsets[0]);
            boneAnim.RotationTimes = ByteArray.ReadInt16List(_data, rotationKeyCount, offsets[1]);
            boneAnim.ScaleTimes = ByteArray.ReadInt16List(_data, scaleKeyCount, offsets[2]);

            boneAnim.TranslationX = ByteArray.ReadFixedList(_data, translationKeyCount, offsets[3]);
            boneAnim.TranslationY = ByteArray.ReadFixedList(_data, translationKeyCount, offsets[4]);
            boneAnim.TranslationZ = ByteArray.ReadFixedList(_data, translationKeyCount, offsets[5]);

            boneAnim.RotationX = ByteArray.ReadFixed16List(_data, rotationKeyCount, offsets[6]);
            boneAnim.RotationY = ByteArray.ReadFixed16List(_data, rotationKeyCount, offsets[7]);
            boneAnim.RotationZ = ByteArray.ReadFixed16List(_data, rotationKeyCount, offsets[8]);
            boneAnim.RotationW = ByteArray.ReadFixed16List(_data, rotationKeyCount, offsets[9]);

            boneAnim.ScaleX = ByteArray.ReadFixedList(_data, scaleKeyCount, offsets[10]);
            boneAnim.ScaleY = ByteArray.ReadFixedList(_data, scaleKeyCount, offsets[11]);
            boneAnim.ScaleZ = ByteArray.ReadFixedList(_data, scaleKeyCount, offsets[12]);

            // fix quaternions for interpolation
            for (int count = 0; count < boneAnim.RotationX.Count; count++)
            {
                if (count == 0)
                {
                    continue;
                }

                Quaternion previous = new Quaternion(boneAnim.RotationX[count - 1],
                                                    boneAnim.RotationY[count - 1],
                                                    boneAnim.RotationZ[count - 1],
                                                    boneAnim.RotationW[count - 1]);

                Quaternion current = new Quaternion(boneAnim.RotationX[count],
                                                    boneAnim.RotationY[count],
                                                    boneAnim.RotationZ[count],
                                                    boneAnim.RotationW[count]);

                if (Quaternion.Dot(current, previous) < 0.0f)
                {
                    boneAnim.RotationX[count] = -boneAnim.RotationX[count];
                    boneAnim.RotationY[count] = -boneAnim.RotationY[count];
                    boneAnim.RotationZ[count] = -boneAnim.RotationZ[count];
                    boneAnim.RotationW[count] = -boneAnim.RotationW[count];
                }
            }

            return boneAnim;
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

            //parent.transform.eulerAngles = new Vector3(180f, 0f, 0f);
            //parent.transform.localScale = new Vector3(-0.1f, 0.1f, 0.1f);

            CreateAndApplyHierarchy(root);

            parent.transform.localScale = new Vector3(0.175f, -0.175f, 0.175f);

            // dont receive shadows
            Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.receiveShadows = false;
            }

            return parent;
        }

        void CreateAndApplyHierarchy(GameObject root)
        {
            int nodeIndex = 0;

            foreach (HierarchyNode node in _hierarchy)
            {
                GameObject nodeObject = new GameObject("node" + nodeIndex);

                if (node.ParentIndex != -1)
                {
                    nodeObject.transform.parent = _hierarchy[node.ParentIndex].NodeObject.transform;
                }
                else
                {
                    nodeObject.transform.parent = root.transform;
                }

                foreach (int partIndex in node.PartList)
                {
                    if (_model.GetPartParent(partIndex) != null)
                    {
                        _model.GetPartParent(partIndex).transform.parent = nodeObject.transform;
                    }
                }

                foreach (SubNode subNode in node.SubNodes)
                {
                    GameObject subNodeObject = new GameObject("subnode" + subNode.Id);
                    subNodeObject.transform.parent = nodeObject.transform;
                    subNodeObject.transform.localPosition = subNode.Translation;
                    subNodeObject.transform.localRotation = subNode.Rotation;
                    subNodeObject.transform.localScale = subNode.Scale;

                    if (subNode.Id == 0x30) // weapon?
                    {
                        int weaponPartIndex = _model.Parts.Count - 1;
                        Transform weaponTransform = _model.GetPartParent(weaponPartIndex).transform;
                        weaponTransform.parent = subNodeObject.transform;
                        weaponTransform.localPosition = Vector3.zero;
                        weaponTransform.localRotation = Quaternion.identity;
                    }
                }

                _hierarchy[nodeIndex].NodeObject = nodeObject;

                nodeIndex++;
            }
        }

        public Animation CreateAnimations(GameObject gameObject)
        {
            Animation anim = gameObject.AddComponent<Animation>();

            int animNr = 0;

            foreach (AnimationDef animDef in _animDefs)
            {
                string animName = "anim" + animNr;

                AnimationClip clip = new AnimationClip();
                clip.legacy = true;

                int boneCount = _boneAnimList.Count;
                int skipTransformCount = 0;

                List<Keyframe>[] translateXList = new List<Keyframe>[boneCount];
                List<Keyframe>[] translateYList = new List<Keyframe>[boneCount];
                List<Keyframe>[] translateZList = new List<Keyframe>[boneCount];

                List<Keyframe>[] rotationXList = new List<Keyframe>[boneCount];
                List<Keyframe>[] rotationYList = new List<Keyframe>[boneCount];
                List<Keyframe>[] rotationZList = new List<Keyframe>[boneCount];
                List<Keyframe>[] rotationWList = new List<Keyframe>[boneCount];

                List<Keyframe>[] scaleXList = new List<Keyframe>[boneCount];
                List<Keyframe>[] scaleYList = new List<Keyframe>[boneCount];
                List<Keyframe>[] scaleZList = new List<Keyframe>[boneCount];

                bool foundKeyframes = false;

                float fps = 25f;

                for (int count = 0; count < boneCount; count++)
                {
                    //Debug.Log("bone: " + count);

                    BoneAnimation boneAnim = _boneAnimList[count];

                    translateXList[count] = new List<Keyframe>();
                    translateYList[count] = new List<Keyframe>();
                    translateZList[count] = new List<Keyframe>();

                    rotationXList[count] = new List<Keyframe>();
                    rotationYList[count] = new List<Keyframe>();
                    rotationZList[count] = new List<Keyframe>();
                    rotationWList[count] = new List<Keyframe>();

                    scaleXList[count] = new List<Keyframe>();
                    scaleYList[count] = new List<Keyframe>();
                    scaleZList[count] = new List<Keyframe>();

                    for (int frameIndex = 0; frameIndex < boneAnim.TranslationTimes.Count; frameIndex++)
                    {
                        int frame = boneAnim.TranslationTimes[frameIndex];
                        //Debug.Log("trn: " + frame);

                        if (frame >= animDef.StartFrame && frame < (animDef.StartFrame + animDef.Frames))
                        {
                            float frameTime = ((frame - animDef.StartFrame) - 1) * (1f / fps);

                            Keyframe animKey;
                            animKey = new Keyframe(frameTime, boneAnim.TranslationX[frameIndex]);
                            translateXList[count].Add(animKey);
                            animKey = new Keyframe(frameTime, boneAnim.TranslationY[frameIndex]);
                            translateYList[count].Add(animKey);
                            animKey = new Keyframe(frameTime, boneAnim.TranslationZ[frameIndex]);
                            translateZList[count].Add(animKey);

                            foundKeyframes = true;
                        }
                    }

                    for (int frameIndex = 0; frameIndex < boneAnim.RotationTimes.Count; frameIndex++)
                    {
                        int frame = boneAnim.RotationTimes[frameIndex];
                        //Debug.Log("rot: " + frame);

                        if (frame >= animDef.StartFrame && frame < (animDef.StartFrame + animDef.Frames))
                        {
                            float frameTime = ((frame - animDef.StartFrame) - 1) * (1f / fps);

                            Keyframe animKey;
                            animKey = new Keyframe(frameTime, boneAnim.RotationX[frameIndex]);
                            rotationXList[count].Add(animKey);
                            animKey = new Keyframe(frameTime, boneAnim.RotationY[frameIndex]);
                            rotationYList[count].Add(animKey);
                            animKey = new Keyframe(frameTime, boneAnim.RotationZ[frameIndex]);
                            rotationZList[count].Add(animKey);
                            animKey = new Keyframe(frameTime, boneAnim.RotationW[frameIndex]);
                            rotationWList[count].Add(animKey);

                            foundKeyframes = true;
                        }
                    }

                    for (int frameIndex = 0; frameIndex < boneAnim.ScaleTimes.Count; frameIndex++)
                    {
                        int frame = boneAnim.ScaleTimes[frameIndex];
                        //Debug.Log("scl: " + frame);

                        if (frame >= animDef.StartFrame && frame < (animDef.StartFrame + animDef.Frames))
                        {
                            float frameTime = ((frame - animDef.StartFrame) - 1) * (1f / fps);

                            Keyframe animKey;
                            animKey = new Keyframe(frameTime, boneAnim.ScaleX[frameIndex]);
                            scaleXList[count].Add(animKey);
                            animKey = new Keyframe(frameTime, boneAnim.ScaleY[frameIndex]);
                            scaleYList[count].Add(animKey);
                            animKey = new Keyframe(frameTime, boneAnim.ScaleZ[frameIndex]);
                            scaleZList[count].Add(animKey);

                            foundKeyframes = true;
                        }
                    }

                    AnimationCurve curve;
                    Transform transformObj;
                    transformObj = _hierarchy[count].NodeObject.transform;
                    string childPath = GetChildPath(gameObject.transform, transformObj);

                    // translation
                    curve = new AnimationCurve();
                    curve.keys = translateXList[count].ToArray();
                    curve = ConvertCurveLinear(curve);
                    clip.SetCurve(childPath, typeof(Transform), "m_LocalPosition.x", curve);

                    curve = new AnimationCurve();
                    curve.keys = translateYList[count].ToArray();
                    curve = ConvertCurveLinear(curve);
                    clip.SetCurve(childPath, typeof(Transform), "m_LocalPosition.y", curve);

                    curve = new AnimationCurve();
                    curve.keys = translateZList[count].ToArray();
                    curve = ConvertCurveLinear(curve);
                    clip.SetCurve(childPath, typeof(Transform), "m_LocalPosition.z", curve);

                    // rotation
                    curve = new AnimationCurve();
                    curve.keys = rotationXList[count].ToArray();
                    curve = ConvertCurveLinear(curve);
                    clip.SetCurve(childPath, typeof(Transform), "m_LocalRotation.x", curve);

                    curve = new AnimationCurve();
                    curve.keys = rotationYList[count].ToArray();
                    curve = ConvertCurveLinear(curve);
                    clip.SetCurve(childPath, typeof(Transform), "m_LocalRotation.y", curve);

                    curve = new AnimationCurve();
                    curve.keys = rotationZList[count].ToArray();
                    curve = ConvertCurveLinear(curve);
                    clip.SetCurve(childPath, typeof(Transform), "m_LocalRotation.z", curve);

                    curve = new AnimationCurve();
                    curve.keys = rotationWList[count].ToArray();
                    curve = ConvertCurveLinear(curve);
                    clip.SetCurve(childPath, typeof(Transform), "m_LocalRotation.w", curve);

                    // scale
                    curve = new AnimationCurve();
                    curve.keys = scaleXList[count].ToArray();
                    curve = ConvertCurveLinear(curve);
                    clip.SetCurve(childPath, typeof(Transform), "m_LocalScale.x", curve);

                    curve = new AnimationCurve();
                    curve.keys = scaleYList[count].ToArray();
                    curve = ConvertCurveLinear(curve);
                    clip.SetCurve(childPath, typeof(Transform), "m_LocalScale.y", curve);

                    curve = new AnimationCurve();
                    curve.keys = scaleZList[count].ToArray();
                    curve = ConvertCurveLinear(curve);
                    clip.SetCurve(childPath, typeof(Transform), "m_LocalScale.z", curve);
                }

                if (foundKeyframes == false)
                {
                    continue;   // no keyframes found for this animation
                }

                clip.frameRate = fps;
                //clip.EnsureQuaternionContinuity();
                clip.wrapMode = WrapMode.Loop;
                clip.legacy = true;
                anim.AddClip(clip, animName);

                animNr++;

                anim[animName].speed = 1f;
            }

            return anim;
        }

        private AnimationCurve ConvertCurveLinear(AnimationCurve curve) 
    {
            AnimationCurve outCurve = new AnimationCurve();

            for (int count_key = 0; count_key < curve.keys.Length; count_key++)
            {
                float intangent = 0f;
                float outtangent = 0f;
                bool intangent_set = false;
                bool outtangent_set = false;
                Vector2 point1;
                Vector2 point2;
                Vector2 deltapoint;
                Keyframe key = curve[count_key];
        
                if (count_key == 0)
                {
                    intangent = 0f;
                    intangent_set = true;
                }

                if (count_key == curve.keys.Length -1)
                {
                    outtangent = 0f;
                    outtangent_set = true;
                }
        
                if (!intangent_set)
                {
                    point1.x = curve.keys[count_key - 1].time;
                    point1.y = curve.keys[count_key - 1].value;
                    point2.x = curve.keys[count_key].time;
                    point2.y = curve.keys[count_key].value;
                
                    deltapoint = point2-point1;
            
                    intangent = deltapoint.y/deltapoint.x;
                }
                if (!outtangent_set)
                {
                    point1.x = curve.keys[count_key].time;
                    point1.y = curve.keys[count_key].value;
                    point2.x = curve.keys[count_key + 1].time;
                    point2.y = curve.keys[count_key + 1].value;
                
                    deltapoint = point2-point1;
                
                    outtangent = deltapoint.y/deltapoint.x;
                }
                
                key.inTangent = intangent;
                key.outTangent = outtangent;
                outCurve.AddKey(key);
            }

            return outCurve;
        }

        private string GetChildPath(Transform rootTransform, Transform childTransform)
        {
            string path = childTransform.name;
            while (childTransform.parent != rootTransform)
            {
                childTransform = childTransform.parent;
                path = childTransform.name + "/" + path;
            }
            return path;
        }
    }
}
