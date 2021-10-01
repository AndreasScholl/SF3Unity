using System.Collections.Generic;
using UnityEngine;

namespace Model
{
    public class ModelPart
    {
        public enum PolygonType
        {
            Opaque,
            Cutout,
            Transparent
        }

        public class MeshData
        {
            public List<Vector3> Vertices = new List<Vector3>();
            public List<Vector3> Normals = new List<Vector3>();
            public List<int> Indices = new List<int>();
            public List<Color> Colors = new List<Color>();
            public List<Vector2> Uvs = new List<Vector2>();
        }

        //public List<Vector3> Vertices;
        //public List<Vector3> VerticesTransparent;
        //public List<Vector3> Normals;
        //public List<Vector3> NormalsTransparent;
        //public List<Color> Colors;
        //public List<Color> ColorsTransparent;
        //public List<Vector2> Uvs;
        //public List<Vector2> UvsTransparent;
        //public List<int> Indices;
        //public List<int> IndicesTransparent;

        MeshData _opaqueMesh = new MeshData();
        MeshData _transparentMesh = new MeshData();
        Dictionary<int, MeshData> _animatedMeshes = new Dictionary<int, MeshData>();

        public List<PolygonType> Types;

        public Vector3 Translation;
        public int Parent;
        public GameObject OpaqueObject;
        public GameObject TransparentObject;
        public List<GameObject> AnimatedObjects = new List<GameObject>();
        public GameObject Pivot;

        public void Init()
        {
            //Vertices = new List<Vector3>();
            //VerticesTransparent = new List<Vector3>();
            //Normals = new List<Vector3>();
            //NormalsTransparent = new List<Vector3>();
            //Indices = new List<int>();
            //IndicesTransparent = new List<int>();
            //Colors = new List<Color>();
            //ColorsTransparent = new List<Color>();
            //Uvs = new List<Vector2>();
            //UvsTransparent = new List<Vector2>();

            Types = new List<PolygonType>();

            Translation = Vector3.zero;
            Parent = -1;
            OpaqueObject = null;
            TransparentObject = null;
        }

        public int GetNumPolygons()
        {
            return Types.Count;
        }

        public Mesh CreateMesh()
        {
            Mesh mesh = CreateMeshFromMeshData(_opaqueMesh);

            return mesh;
        }

        public Mesh CreateTransparentMesh()
        {
            Mesh mesh = CreateMeshFromMeshData(_transparentMesh);

            return mesh;
        }

        public Dictionary<int, Mesh> CreateAnimatedMeshes()
        {
            Dictionary<int, Mesh> meshes = new Dictionary<int, Mesh>();

            foreach (KeyValuePair<int, MeshData> pair in _animatedMeshes)
            {
                Mesh mesh = CreateMeshFromMeshData(pair.Value);
                meshes[pair.Key] = mesh;
            }

            return meshes;
        }

        public Mesh CreateMeshFromMeshData(MeshData meshData)
        {
            if (meshData.Vertices.Count == 0)
            {
                return null;
            }

            Mesh mesh = new Mesh();

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(meshData.Vertices);
            mesh.SetColors(meshData.Colors);
            mesh.SetNormals(meshData.Normals);
            mesh.SetUVs(0, meshData.Uvs);
            mesh.SetIndices(meshData.Indices.ToArray(), MeshTopology.Triangles, 0);

            mesh.name = "mesh";

            return mesh;
        }

        public void AddPolygon(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                       bool halfTransparent, bool doubleSided, Color colorA, Color colorB, Color colorC, Color colorD,
                       Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD,
                       Vector3 nA, Vector3 nB, Vector3 nC, Vector3 nD, int sub = 1, bool vert = true, int animationGroup = -1)
        {
            Vector3 a1, b1, c1, d1;
            Vector3 uvA1, uvB1, uvC1, uvD1;
            Vector3 nA1, nB1, nC1, nD1;

            for (int s = 0; s < sub; s++)
            {
                float subSize = 1f / (float)sub;
                float lStart = (float)s * subSize;
                float lEnd = lStart + subSize;

                if (vert == true)
                {
                    a1 = Vector3.Lerp(a, b, lStart);
                    b1 = Vector3.Lerp(a, b, lEnd);
                    d1 = Vector3.Lerp(d, c, lStart);
                    c1 = Vector3.Lerp(d, c, lEnd);

                    uvA1 = Vector3.Lerp(uvA, uvB, lStart);
                    uvB1 = Vector3.Lerp(uvA, uvB, lEnd);
                    uvD1 = Vector3.Lerp(uvD, uvC, lStart);
                    uvC1 = Vector3.Lerp(uvD, uvC, lEnd);

                    nA1 = Vector3.Lerp(nA, nB, lStart);
                    nB1 = Vector3.Lerp(nA, nB, lEnd);
                    nD1 = Vector3.Lerp(nD, nC, lStart);
                    nC1 = Vector3.Lerp(nD, nC, lEnd);

                    AddPolygon(a1, b1, c1, d1, halfTransparent, doubleSided, colorA, colorB, colorC, colorD,
                                           uvA1, uvB1, uvC1, uvD1, nA1, nB1, nC1, nD1, sub, false, animationGroup);
                }
                else
                {
                    a1 = Vector3.Lerp(a, d, lStart);
                    b1 = Vector3.Lerp(b, c, lStart);
                    d1 = Vector3.Lerp(a, d, lEnd);
                    c1 = Vector3.Lerp(b, c, lEnd);

                    uvA1 = Vector3.Lerp(uvA, uvD, lStart);
                    uvB1 = Vector3.Lerp(uvB, uvC, lStart);
                    uvD1 = Vector3.Lerp(uvA, uvD, lEnd);
                    uvC1 = Vector3.Lerp(uvB, uvC, lEnd);

                    nA1 = Vector3.Lerp(nA, nD, lStart);
                    nB1 = Vector3.Lerp(nB, nC, lStart);
                    nD1 = Vector3.Lerp(nA, nD, lEnd);
                    nC1 = Vector3.Lerp(nB, nC, lEnd);

                    AddSubPolygon(a1, b1, c1, d1, halfTransparent, doubleSided, colorA, colorB, colorC, colorD,
                                           uvA1, uvB1, uvC1, uvD1, nA1, nB1, nC1, nD1, animationGroup);
                }
            }
        }

        public void AddSubPolygon(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                               bool halfTransparent, bool doubleSided, Color colorA, Color colorB, Color colorC, Color colorD,
                               Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD,
                               Vector3 nA, Vector3 nB, Vector3 nC, Vector3 nD, int animationGroup = -1)
        {
            List<Vector3> vertices;
            List<int> indices;
            List<Vector3> normals;
            List<Color> colors;
            List<Vector2> uvs;

            PolygonType polygonType = PolygonType.Opaque;

            MeshData meshData;

            // get meshData storage depending on type of polygon
            if (halfTransparent == false)
            {
                if (animationGroup == -1)
                {
                    meshData = _opaqueMesh;
                }
                else
                {
                    // handle animationGroups
                    meshData = GetAnimationMeshDataByGroup(animationGroup);
                }
            }
            else
            {
                if (animationGroup == -1)
                {
                    meshData = _transparentMesh;
                }
                else
                {
                    // handle animationGroups
                    meshData = GetAnimationMeshDataByGroup(animationGroup);
                }

                polygonType = PolygonType.Transparent;
            }

            vertices = meshData.Vertices;
            indices = meshData.Indices;
            normals = meshData.Normals;
            colors = meshData.Colors;
            uvs = meshData.Uvs;

            Types.Add(polygonType);

            // add vertices, normals and indices
            //
            int idxA = vertices.Count;
            vertices.Add(a);
            normals.Add(nA);

            int idxB = vertices.Count;
            vertices.Add(b);
            normals.Add(nB);

            int idxC = vertices.Count;
            vertices.Add(c);
            normals.Add(nC);

            int idxD = vertices.Count;
            vertices.Add(d);
            normals.Add(nD);

            indices.Add(idxA);    // triangle 1
            indices.Add(idxB);
            indices.Add(idxC);

            indices.Add(idxA);    // triangle 2
            indices.Add(idxC);
            indices.Add(idxD);

            if (doubleSided == true)
            {
                indices.Add(idxC);    // reversed triangle 1
                indices.Add(idxB);
                indices.Add(idxA);

                indices.Add(idxD);    // reversed triangle 2
                indices.Add(idxC);
                indices.Add(idxA);
            }

            uvs.Add(uvA);
            uvs.Add(uvB);
            uvs.Add(uvC);
            uvs.Add(uvD);

            colors.Add(colorA);
            colors.Add(colorB);
            colors.Add(colorC);
            colors.Add(colorD);
        }

        //public void AddTriangle(Vector3 a, Vector3 b, Vector3 c,
        //                       bool halfTransparent, bool doubleSided, Color colorA, Color colorB, Color colorC,
        //                       Vector2 uvA, Vector2 uvB, Vector2 uvC,
        //                       Vector3 nA, Vector3 nB, Vector3 nC)
        //{
        //    List<Vector3> vertices;
        //    List<int> indices;
        //    List<Vector3> normals;
        //    List<Color> colors;
        //    List<Vector2> uvs;

        //    PolygonType polygonType = PolygonType.Opaque;

        //    // get lists depending on transparency mode
        //    if (halfTransparent == false)
        //    {
        //        vertices = Vertices;
        //        indices = Indices;
        //        normals = Normals;
        //        colors = Colors;
        //        uvs = Uvs;
        //    }
        //    else
        //    {
        //        vertices = VerticesTransparent;
        //        indices = IndicesTransparent;
        //        normals = NormalsTransparent;
        //        colors = ColorsTransparent;
        //        uvs = UvsTransparent;

        //        polygonType = PolygonType.Transparent;
        //    }
        //    Types.Add(polygonType);

        //    // add vertices, normals and indices
        //    //
        //    int idxA = vertices.Count;
        //    vertices.Add(a);
        //    normals.Add(nA);

        //    int idxB = vertices.Count;
        //    vertices.Add(b);
        //    normals.Add(nB);

        //    int idxC = vertices.Count;
        //    vertices.Add(c);
        //    normals.Add(nC);

        //    indices.Add(idxA);    // triangle 1
        //    indices.Add(idxB);
        //    indices.Add(idxC);

        //    if (doubleSided == true)
        //    {
        //        indices.Add(idxC);    // reversed triangle 1
        //        indices.Add(idxB);
        //        indices.Add(idxA);
        //    }

        //    uvs.Add(uvA);
        //    uvs.Add(uvB);
        //    uvs.Add(uvC);

        //    colors.Add(colorA);
        //    colors.Add(colorB);
        //    colors.Add(colorC);
        //}

        private MeshData GetAnimationMeshDataByGroup(int animationGroup)
        {
            if (_animatedMeshes.ContainsKey(animationGroup))
            {
                return _animatedMeshes[animationGroup];
            }

            MeshData meshData = new MeshData();
            _animatedMeshes[animationGroup] = meshData;

            return meshData;
        }

        public GameObject Instantiate(Color[] colors)
        {
            GameObject obj = null;

            List<Color> colorOpaque = new List<Color>();
            List<Color> colorTransparent = new List<Color>();

            int count = 0;
            foreach (Color color in colors)
            {
                if (Types[count / 4] == PolygonType.Opaque)
                {
                    colorOpaque.Add(color);
                }
                else
                {
                    colorTransparent.Add(color);
                    //colorTransparent.Add(Color.black);
                }

                count++;
            }

            obj = GameObject.Instantiate(OpaqueObject);

            // apply opaque colors
            if (colorOpaque.Count > 0)
            {
                MeshFilter filter = obj.GetComponent<MeshFilter>();
                Mesh mesh = CopyMesh(filter.mesh);
                mesh.colors = colorOpaque.ToArray();
                filter.mesh = mesh;
            }

            if (TransparentObject != null)
            {
                GameObject transpObj = GameObject.Instantiate(TransparentObject);
                transpObj.transform.parent = obj.transform;
                transpObj.transform.localPosition = Vector3.zero;
                transpObj.transform.localEulerAngles = Vector3.zero;

                MeshFilter filter = transpObj.GetComponent<MeshFilter>();
                Mesh mesh = CopyMesh(filter.mesh);
                mesh.colors = colorTransparent.ToArray();
                filter.mesh = mesh;
            }

            return obj;
        }

        private Mesh CopyMesh(Mesh source)
        {
            Mesh mesh = source;
            Mesh copy = new Mesh();
            copy.vertices = mesh.vertices;
            copy.triangles = mesh.triangles;
            copy.uv = mesh.uv;
            copy.normals = mesh.normals;
            copy.colors = mesh.colors;
            copy.tangents = mesh.tangents;

            return copy;
        }

        public void AddCollider(int layer)
        {
            if (OpaqueObject != null)
            {
                if (OpaqueObject.GetComponent<MeshFilter>() != null)
                {
                    MeshCollider collider = OpaqueObject.AddComponent<MeshCollider>();
                    OpaqueObject.layer = layer;
                }
            }

            if (TransparentObject != null)
            {
                MeshCollider collider = TransparentObject.AddComponent<MeshCollider>();
                TransparentObject.layer = layer;
            }
        }

        public void EnableRenderers(bool enable)
        {
            Renderer[] renderers = Pivot.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = enable;
            }
        }
    }
}
