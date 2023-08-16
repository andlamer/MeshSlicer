using UnityEngine;
using System.Collections.Generic;

namespace MeshSlicer.MeshCombining
{
    public static class MeshExtension
    {
        public static Mesh GetMeshDataFromSubMesh(this Mesh aMesh, int aSubMeshIndex)
        {
            if (aSubMeshIndex < 0 || aSubMeshIndex >= aMesh.subMeshCount)
                return null;

            var indices = aMesh.GetTriangles(aSubMeshIndex);
            var source = new Vertices(aMesh);
            var dest = new Vertices();
            var map = new Dictionary<int, int>();
            var newIndices = new int[indices.Length];

            for (var i = 0; i < indices.Length; i++)
            {
                var o = indices[i];

                if (!map.TryGetValue(o, out var n))
                {
                    n = dest.Add(source, o);
                    map.Add(o, n);
                }

                newIndices[i] = n;
            }

            var m = new Mesh();
            dest.AssignTo(m);
            m.triangles = newIndices;

            return m;
        }

        private class Vertices
        {
            private List<Vector3> _verts;
            private List<Vector2> _uv1;
            private List<Vector2> _uv2;
            private List<Vector2> _uv3;
            private List<Vector2> _uv4;
            private List<Vector3> _normals;
            private List<Vector4> _tangents;
            private List<Color32> _colors;
            private List<BoneWeight> _boneWeights;

            public Vertices()
            {
                _verts = new List<Vector3>();
            }

            public Vertices(Mesh aMesh)
            {
                _verts = CreateList(aMesh.vertices);
                _uv1 = CreateList(aMesh.uv);
                _uv2 = CreateList(aMesh.uv2);
                _uv3 = CreateList(aMesh.uv3);
                _uv4 = CreateList(aMesh.uv4);
                _normals = CreateList(aMesh.normals);
                _tangents = CreateList(aMesh.tangents);
                _colors = CreateList(aMesh.colors32);
                _boneWeights = CreateList(aMesh.boneWeights);
            }

            private static List<T> CreateList<T>(T[] aSource)
            {
                if (aSource == null || aSource.Length == 0)
                    return null;

                return new List<T>(aSource);
            }

            private static void Copy<T>(ref List<T> aDest, List<T> aSource, int aIndex)
            {
                if (aSource == null)
                    return;

                aDest ??= new List<T>();
                aDest.Add(aSource[aIndex]);
            }

            public int Add(Vertices aOther, int aIndex)
            {
                var i = _verts.Count;
                Copy(ref _verts, aOther._verts, aIndex);
                Copy(ref _uv1, aOther._uv1, aIndex);
                Copy(ref _uv2, aOther._uv2, aIndex);
                Copy(ref _uv3, aOther._uv3, aIndex);
                Copy(ref _uv4, aOther._uv4, aIndex);
                Copy(ref _normals, aOther._normals, aIndex);
                Copy(ref _tangents, aOther._tangents, aIndex);
                Copy(ref _colors, aOther._colors, aIndex);
                Copy(ref _boneWeights, aOther._boneWeights, aIndex);
                return i;
            }

            public void AssignTo(Mesh aTarget)
            {
                if (_verts.Count > 65535)
                    aTarget.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                aTarget.SetVertices(_verts);

                if (_uv1 != null) aTarget.SetUVs(0, _uv1);
                if (_uv2 != null) aTarget.SetUVs(1, _uv2);
                if (_uv3 != null) aTarget.SetUVs(2, _uv3);
                if (_uv4 != null) aTarget.SetUVs(3, _uv4);
                if (_normals != null) aTarget.SetNormals(_normals);
                if (_tangents != null) aTarget.SetTangents(_tangents);
                if (_colors != null) aTarget.SetColors(_colors);
                if (_boneWeights != null) aTarget.boneWeights = _boneWeights.ToArray();
            }
        }
    }
}
