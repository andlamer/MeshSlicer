using System;
using System.Collections.Generic;
using System.Linq;
using MeshSlicer.CustomTriangulation;
using UnityEngine;

namespace MeshSlicer.Slicer
{
    /// <summary>
    /// The side of the mesh
    /// </summary>
    public enum MeshSide
    {
        Positive = 0,
        Negative = 1
    }

    /// <summary>
    /// An object used to manage the positive and negative side mesh data for a sliced object
    /// </summary>
    internal class SlicesMetadata
    {
        private const float IgnoredDistance = 0.0001f;

        private Mesh _positiveSideMesh;
        private List<Vector3> _positiveSideVertices;
        private List<int> _positiveSideTriangles;
        private List<Vector2> _positiveSideUvs;
        private List<Vector3> _positiveSideNormals;

        private Mesh _negativeSideMesh;
        private List<Vector3> _negativeSideVertices;
        private List<int> _negativeSideTriangles;
        private List<Vector2> _negativeSideUvs;
        private List<Vector3> _negativeSideNormals;

        private readonly List<(Vector3, Vector3)> _edgesAlongPlane;
        private readonly Dictionary<Vector3, Vector2> _pointsAlongPlaneUVs;

        private Plane _plane;
        private readonly Mesh _mesh;
        private readonly bool _useSharedVertices;
        private readonly bool _smoothVertices;
        private readonly bool _createReverseTriangleWindings;

        private bool IsSolid { get; }

        public Mesh PositiveSideMesh
        {
            get
            {
                if (_positiveSideMesh == null)
                {
                    _positiveSideMesh = new Mesh();
                }

                SetMeshData(MeshSide.Positive);
                return _positiveSideMesh;
            }
        }

        public Mesh NegativeSideMesh
        {
            get
            {
                if (_negativeSideMesh == null)
                {
                    _negativeSideMesh = new Mesh();
                }

                SetMeshData(MeshSide.Negative);

                return _negativeSideMesh;
            }
        }

        public SlicesMetadata(Plane plane, Mesh mesh, bool isSolid, bool createReverseTriangleWindings, bool shareVertices, bool smoothVertices)
        {
            _positiveSideTriangles = new List<int>();
            _positiveSideVertices = new List<Vector3>();
            _negativeSideTriangles = new List<int>();
            _negativeSideVertices = new List<Vector3>();
            _positiveSideUvs = new List<Vector2>();
            _negativeSideUvs = new List<Vector2>();
            _positiveSideNormals = new List<Vector3>();
            _negativeSideNormals = new List<Vector3>();
            _edgesAlongPlane = new List<(Vector3, Vector3)>();
            _pointsAlongPlaneUVs = new Dictionary<Vector3, Vector2>();
            _plane = plane;
            _mesh = mesh;
            IsSolid = isSolid;
            _createReverseTriangleWindings = createReverseTriangleWindings;
            _useSharedVertices = shareVertices;
            _smoothVertices = smoothVertices;
        }

        /// <summary>
        /// Add the mesh data to the correct side and calculate normals
        /// </summary>
        /// <param name="side"></param>
        /// <param name="vertex1"></param>
        /// <param name="uv1"></param>
        /// <param name="vertex2"></param>
        /// <param name="uv2"></param>
        /// <param name="vertex3"></param>
        /// <param name="normal3"></param>
        /// <param name="uv3"></param>
        /// <param name="shareVertices"></param>
        /// <param name="addFirst"></param>
        /// <param name="normal1"></param>
        /// <param name="normal2"></param>
        private void AddTrianglesNormalAndUvs(MeshSide side, Vector3 vertex1, Vector3? normal1, Vector2 uv1, Vector3 vertex2, Vector3? normal2, Vector2 uv2, Vector3 vertex3,
            Vector3? normal3, Vector2 uv3, bool shareVertices, bool addFirst)
        {
            if (side == MeshSide.Positive)
            {
                AddTrianglesNormalsAndUvs(ref _positiveSideVertices, ref _positiveSideTriangles, ref _positiveSideNormals, ref _positiveSideUvs, vertex1, normal1, uv1, vertex2,
                    normal2, uv2, vertex3, normal3, uv3, shareVertices, addFirst);
            }
            else
            {
                AddTrianglesNormalsAndUvs(ref _negativeSideVertices, ref _negativeSideTriangles, ref _negativeSideNormals, ref _negativeSideUvs, vertex1, normal1, uv1, vertex2,
                    normal2, uv2, vertex3, normal3, uv3, shareVertices, addFirst);
            }
        }


        /// <summary>
        /// Adds the vertices to the mesh sets the triangles in the order that the vertices are provided.
        /// If shared vertices is false vertices will be added to the list even if a matching vertex already exists
        /// Does not compute normals
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="triangles"></param>
        /// <param name="uvs"></param>
        /// <param name="normals"></param>
        /// <param name="vertex1"></param>
        /// <param name="normal1"></param>
        /// <param name="uv1"></param>
        /// <param name="vertex2"></param>
        /// <param name="normal2"></param>
        /// <param name="uv2"></param>
        /// <param name="vertex3"></param>
        /// <param name="normal3"></param>
        /// <param name="uv3"></param>
        /// <param name="shareVertices"></param>
        /// <param name="addFirst"></param>
        private void AddTrianglesNormalsAndUvs(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs, Vector3 vertex1,
            Vector3? normal1, Vector2 uv1, Vector3 vertex2, Vector3? normal2, Vector2 uv2, Vector3 vertex3, Vector3? normal3, Vector2 uv3, bool shareVertices, bool addFirst)
        {
            if (addFirst)
            {
                ShiftTriangleIndices(ref triangles);
            }

            var tri1Index = vertices.IndexOf(vertex1);
            var exactSameVector1AlreadyExists = tri1Index > -1 && normal1.HasValue && normals[tri1Index] == normal1.Value && uvs[tri1Index] == uv1;

            //If a the vertex already exists we just add a triangle reference to it, if not add the vert to the list and then add the tri index
            if (shareVertices && exactSameVector1AlreadyExists)
            {
                triangles.Add(tri1Index);
            }
            else
            {
                normal1 ??= ComputeNormal(vertex1, vertex2, vertex3);

                int? i = null;
                if (addFirst)
                {
                    i = 0;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex1, (Vector3) normal1, uv1, i);
            }

            var tri2Index = vertices.IndexOf(vertex2);
            var exactSameVector2AlreadyExists = tri2Index > -1 && normal2.HasValue && normals[tri2Index] == normal2.Value && uvs[tri2Index] == uv2;

            if (shareVertices && exactSameVector2AlreadyExists)
            {
                triangles.Add(tri2Index);
            }
            else
            {
                normal2 ??= ComputeNormal(vertex2, vertex3, vertex1);

                int? i = null;

                if (addFirst)
                {
                    i = 1;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex2, (Vector3) normal2, uv2, i);
            }

            var tri3Index = vertices.IndexOf(vertex3);
            var exactSameVector3AlreadyExists = tri3Index > -1 && normal3.HasValue && normals[tri3Index] == normal3.Value && uvs[tri3Index] == uv3;

            if (shareVertices && exactSameVector3AlreadyExists)
            {
                triangles.Add(tri3Index);
            }
            else
            {
                normal3 ??= ComputeNormal(vertex3, vertex1, vertex2);

                int? i = null;
                if (addFirst)
                {
                    i = 2;
                }

                AddVertNormalUv(ref vertices, ref normals, ref uvs, ref triangles, vertex3, (Vector3) normal3, uv3, i);
            }
        }

        private static void AddVertNormalUv(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<Vector2> uvs, ref List<int> triangles, Vector3 vertex, Vector3 normal,
            Vector2 uv, int? index)
        {
            if (index != null)
            {
                var i = (int) index;
                vertices.Insert(i, vertex);
                uvs.Insert(i, uv);
                normals.Insert(i, normal);
                triangles.Insert(i, i);
            }
            else
            {
                vertices.Add(vertex);
                normals.Add(normal);
                uvs.Add(uv);
                triangles.Add(vertices.Count - 1);
            }
        }

        private static void ShiftTriangleIndices(ref List<int> triangles)
        {
            for (var j = 0; j < triangles.Count; j += 3)
            {
                triangles[j] += 3;
                triangles[j + 1] += 3;
                triangles[j + 2] += 3;
            }
        }

        /// <summary>
        /// Will render the inside of an object
        /// This is heavy as it duplicates all the vertices and creates opposite winding direction
        /// </summary>
        private void AddReverseTriangleWinding()
        {
            var positiveVertsStartIndex = _positiveSideVertices.Count;
            //Duplicate the original vertices
            _positiveSideVertices.AddRange(_positiveSideVertices);
            _positiveSideUvs.AddRange(_positiveSideUvs);
            _positiveSideNormals.AddRange(FlipNormals(_positiveSideNormals));

            var numPositiveTriangles = _positiveSideTriangles.Count;

            //Add reverse windings
            for (var i = 0; i < numPositiveTriangles; i += 3)
            {
                _positiveSideTriangles.Add(positiveVertsStartIndex + _positiveSideTriangles[i]);
                _positiveSideTriangles.Add(positiveVertsStartIndex + _positiveSideTriangles[i + 2]);
                _positiveSideTriangles.Add(positiveVertsStartIndex + _positiveSideTriangles[i + 1]);
            }

            var negativeVertexStartIndex = _negativeSideVertices.Count;
            //Duplicate the original vertices
            _negativeSideVertices.AddRange(_negativeSideVertices);
            _negativeSideUvs.AddRange(_negativeSideUvs);
            _negativeSideNormals.AddRange(FlipNormals(_negativeSideNormals));

            var numNegativeTriangles = _negativeSideTriangles.Count;

            //Add reverse windings
            for (var i = 0; i < numNegativeTriangles; i += 3)
            {
                _negativeSideTriangles.Add(negativeVertexStartIndex + _negativeSideTriangles[i]);
                _negativeSideTriangles.Add(negativeVertexStartIndex + _negativeSideTriangles[i + 2]);
                _negativeSideTriangles.Add(negativeVertexStartIndex + _negativeSideTriangles[i + 1]);
            }
        }

        /// <summary>
        /// Join the points along the plane to the halfway point
        /// </summary>
        private void JoinPointsAlongPlane()
        {
            if (_edgesAlongPlane.Count < 2)
                return;

            var triangulation = new Triangulation();
            var positiveTrianglesList = triangulation.GetTriangulationTriangles(_edgesAlongPlane);

            for (var i = 0; i < positiveTrianglesList.Count; i += 3)
            {
                var uv1 = _pointsAlongPlaneUVs.ContainsKey(positiveTrianglesList[i])
                    ? _pointsAlongPlaneUVs[positiveTrianglesList[i]]
                    : Vector2.zero;
                var uv2 = _pointsAlongPlaneUVs.ContainsKey(positiveTrianglesList[i + 1])
                    ? _pointsAlongPlaneUVs[positiveTrianglesList[i + 1]]
                    : Vector2.zero;
                var uv3 = _pointsAlongPlaneUVs.ContainsKey(positiveTrianglesList[i + 2])
                    ? _pointsAlongPlaneUVs[positiveTrianglesList[i + 2]]
                    : Vector2.zero;

                AddTrianglesNormalAndUvs(MeshSide.Positive, positiveTrianglesList[i], _plane.normal * -1, uv1,
                    positiveTrianglesList[i + 1], _plane.normal * -1, uv2,
                    positiveTrianglesList[i + 2], _plane.normal * -1, uv3,
                    false,
                    true);
                AddTrianglesNormalAndUvs(MeshSide.Negative, positiveTrianglesList[i], _plane.normal, uv1,
                    positiveTrianglesList[i + 1], _plane.normal, uv2,
                    positiveTrianglesList[i + 2], _plane.normal, uv3,
                    false,
                    true);
            }
        }

        /// <summary>
        /// Setup the mesh object for the specified side
        /// </summary>
        /// <param name="side"></param>
        private void SetMeshData(MeshSide side)
        {
            if (side == MeshSide.Positive)
            {
                _positiveSideMesh.vertices = _positiveSideVertices.ToArray();
                _positiveSideMesh.triangles = _positiveSideTriangles.ToArray();
                _positiveSideMesh.normals = _positiveSideNormals.ToArray();
                _positiveSideMesh.uv = _positiveSideUvs.ToArray();
            }
            else
            {
                _negativeSideMesh.vertices = _negativeSideVertices.ToArray();
                _negativeSideMesh.triangles = _negativeSideTriangles.ToArray();
                _negativeSideMesh.normals = _negativeSideNormals.ToArray();
                _negativeSideMesh.uv = _negativeSideUvs.ToArray();
            }
        }

        /// <summary>
        /// Compute the positive and negative meshes based on the plane and mesh
        /// </summary>
        public void ComputeNewMeshes()
        {
            var meshTriangles = _mesh.triangles;
            var meshVerts = _mesh.vertices;
            var meshNormals = _mesh.normals;
            var meshUvs = _mesh.uv;

            for (var i = 0; i < meshTriangles.Length; i += 3)
            {
                //We need the verts in order so that we know which way to wind our new mesh triangles.
                var vert1Index = meshTriangles[i];
                var vert1 = meshVerts[vert1Index];
                var uv1 = meshUvs[vert1Index];
                var normal1 = meshNormals[vert1Index];
                var vert1Side = _plane.GetSide(vert1);

                var vert2Index = meshTriangles[i + 1];
                var vert2 = meshVerts[vert2Index];
                var uv2 = meshUvs[vert2Index];
                var normal2 = meshNormals[vert2Index];
                var vert2Side = _plane.GetSide(vert2);

                var vert3Index = meshTriangles[i + 2];
                var vert3 = meshVerts[vert3Index];
                var normal3 = meshNormals[vert3Index];
                var uv3 = meshUvs[vert3Index];
                var vert3Side = _plane.GetSide(vert3);

                var canIgnoreDistanceToPlane1 = Math.Abs(_plane.GetDistanceToPoint(vert1)) < IgnoredDistance;
                var canIgnoreDistanceToPlane2 = Math.Abs(_plane.GetDistanceToPoint(vert2)) < IgnoredDistance;
                var canIgnoreDistanceToPlane3 = Math.Abs(_plane.GetDistanceToPoint(vert3)) < IgnoredDistance;

                //All verts are on the same side
                if (vert1Side == vert2Side && vert2Side == vert3Side)
                {
                    //Add the relevant triangle
                    var side = vert1Side ? MeshSide.Positive : MeshSide.Negative;
                    AddTrianglesNormalAndUvs(side, vert1, normal1, uv1, vert2, normal2, uv2, vert3, normal3, uv3, true, false);
                }
                else if (canIgnoreDistanceToPlane1 || canIgnoreDistanceToPlane2 || canIgnoreDistanceToPlane3)
                {
                    var allPointsClose = canIgnoreDistanceToPlane1 && canIgnoreDistanceToPlane2 && canIgnoreDistanceToPlane3;

                    if (allPointsClose) continue;

                    var point1And2Close = canIgnoreDistanceToPlane1 && canIgnoreDistanceToPlane2 && !canIgnoreDistanceToPlane3;
                    var point2And3Close = canIgnoreDistanceToPlane2 && canIgnoreDistanceToPlane3 && !canIgnoreDistanceToPlane1;
                    var point1And3Close = canIgnoreDistanceToPlane3 && canIgnoreDistanceToPlane1 && !canIgnoreDistanceToPlane2;

                    var twoPointsClose = point1And2Close || point2And3Close || point1And3Close;

                    if (twoPointsClose)
                    {
                        var meshSideBool = point1And2Close ? vert3Side :
                            point1And3Close ? vert2Side :
                            vert1Side;

                        var edgeAlongPlane = point1And2Close ? (vert1, vert2) :
                            point1And3Close ? (vert1, vert3) :
                            (vert2, vert3);

                        var (firstUv, secondUv) = point1And2Close ? (uv1, uv2) :
                            point1And3Close ? (uv1, uv3) :
                            (uv2, uv3);

                        var side = meshSideBool ? MeshSide.Positive : MeshSide.Negative;

                        AddTrianglesNormalAndUvs(side,
                            vert1, normal1, uv1,
                            vert2, normal2, uv2,
                            vert3, normal3, uv3,
                            _useSharedVertices, false);

                        if (!_edgesAlongPlane.Contains(edgeAlongPlane))
                            _edgesAlongPlane.Add(edgeAlongPlane);
                        if (!_pointsAlongPlaneUVs.ContainsKey(edgeAlongPlane.Item1))
                            _pointsAlongPlaneUVs.Add(edgeAlongPlane.Item1, firstUv);
                        if (!_pointsAlongPlaneUVs.ContainsKey(edgeAlongPlane.Item2))
                            _pointsAlongPlaneUVs.Add(edgeAlongPlane.Item2, secondUv);
                    }
                    else
                    {
                        bool meshSide;
                        bool firstComparisonSide;
                        bool secondComparisonSide;

                        bool multiplyNormals;

                        (Vector3, Vector3, Vector2) firstVertexData;
                        (Vector3, Vector3, Vector2) secondVertexData;
                        (Vector3, Vector3, Vector2) thirdVertexData;

                        if (canIgnoreDistanceToPlane1)
                        {
                            meshSide = vert1Side;
                            firstComparisonSide = vert2Side;
                            secondComparisonSide = vert3Side;

                            firstVertexData = (vert1, normal1, uv1);
                            secondVertexData = vert1Side == vert2Side
                                ? (vert2, normal2, uv2)
                                : (vert3, normal3, uv3);
                            thirdVertexData = vert1Side == vert2Side
                                ? (vert3, normal3, uv3)
                                : (vert2, normal2, uv2);

                            multiplyNormals = vert1Side == vert2Side;
                        }
                        else if (canIgnoreDistanceToPlane2)
                        {
                            meshSide = vert2Side;
                            firstComparisonSide = vert3Side;
                            secondComparisonSide = vert1Side;

                            firstVertexData = (vert2, normal2, uv2);
                            secondVertexData = vert2Side == vert3Side
                                ? (vert3, normal3, uv3)
                                : (vert1, normal1, uv1);
                            thirdVertexData = vert2Side == vert3Side
                                ? (vert1, normal1, uv1)
                                : (vert3, normal3, uv3);

                            multiplyNormals = vert2Side == vert3Side;
                        }
                        else
                        {
                            meshSide = vert3Side;
                            firstComparisonSide = vert1Side;
                            secondComparisonSide = vert2Side;

                            firstVertexData = (vert3, normal3, uv3);
                            secondVertexData = vert3Side == vert1Side
                                ? (vert1, normal1, uv1)
                                : (vert2, normal2, uv2);
                            thirdVertexData = vert3Side == vert1Side
                                ? (vert2, normal2, uv2)
                                : (vert1, normal1, uv1);

                            multiplyNormals = vert1Side == vert3Side;
                        }

                        if (firstComparisonSide == secondComparisonSide)
                        {
                            var side = meshSide ? MeshSide.Negative : MeshSide.Positive;

                            AddTrianglesNormalAndUvs(side,
                                firstVertexData.Item1, firstVertexData.Item2, firstVertexData.Item3,
                                thirdVertexData.Item1, thirdVertexData.Item2, thirdVertexData.Item3,
                                secondVertexData.Item1, secondVertexData.Item2, secondVertexData.Item3,
                                _useSharedVertices, false);
                        }
                        else
                        {
                            var side1 = meshSide ? MeshSide.Positive : MeshSide.Negative;
                            var side2 = meshSide ? MeshSide.Negative : MeshSide.Positive;

                            var intersection = GetRayPlaneIntersectionPointAndUv(thirdVertexData.Item1, thirdVertexData.Item3,
                                secondVertexData.Item1, secondVertexData.Item3,
                                out var intersectionUv);

                            AddTrianglesNormalAndUvs(side1,
                                firstVertexData.Item1, firstVertexData.Item2 * (multiplyNormals ? 1 : -1), firstVertexData.Item3,
                                secondVertexData.Item1, secondVertexData.Item2 * (multiplyNormals ? 1 : -1), secondVertexData.Item3,
                                intersection, null, intersectionUv,
                                _useSharedVertices, false);
                            AddTrianglesNormalAndUvs(side2,
                                firstVertexData.Item1, firstVertexData.Item2 * (multiplyNormals ? 1 : -1), firstVertexData.Item3,
                                intersection, null, intersectionUv,
                                thirdVertexData.Item1, thirdVertexData.Item2 * (multiplyNormals ? 1 : -1), thirdVertexData.Item3,
                                _useSharedVertices, false);

                            if (!_edgesAlongPlane.Contains((firstVertexData.Item1, intersection)))
                                _edgesAlongPlane.Add((firstVertexData.Item1, intersection));
                            if (!_pointsAlongPlaneUVs.ContainsKey(firstVertexData.Item1))
                                _pointsAlongPlaneUVs.Add(firstVertexData.Item1, firstVertexData.Item3);
                            if (!_pointsAlongPlaneUVs.ContainsKey(intersection))
                                _pointsAlongPlaneUVs.Add(intersection, intersectionUv);
                        }
                    }
                }
                else
                {
                    //we need the two points where the plane intersects the triangle.
                    Vector3 intersection1;
                    Vector3 intersection2;

                    Vector2 intersection1Uv;
                    Vector2 intersection2Uv;

                    var side1 = (vert1Side) ? MeshSide.Positive : MeshSide.Negative;
                    var side2 = (vert1Side) ? MeshSide.Negative : MeshSide.Positive;

                    //vert 1 and 2 are on the same side
                    if (vert1Side == vert2Side)
                    {
                        //Cast a ray from v2 to v3 and from v3 to v1 to get the intersections                       
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert2, uv2, vert3, uv3, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert3, uv3, vert1, uv1, out intersection2Uv);

                        //Add the positive or negative triangles
                        AddTrianglesNormalAndUvs(side1, vert1, normal1, uv1, vert2, normal2, uv2, intersection1, null, intersection1Uv, _useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side1, vert1, normal1, uv1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, _useSharedVertices, false);

                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert3, normal3, uv3, intersection2, null, intersection2Uv, _useSharedVertices, false);
                    }
                    //vert 1 and 3 are on the same side
                    else if (vert1Side == vert3Side)
                    {
                        //Cast a ray from v1 to v2 and from v2 to v3 to get the intersections                       
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert2, uv2, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert2, uv2, vert3, uv3, out intersection2Uv);

                        //Add the positive triangles
                        AddTrianglesNormalAndUvs(side1, vert1, normal1, uv1, intersection1, null, intersection1Uv, vert3, normal3, uv3, _useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, vert3, normal3, uv3, _useSharedVertices, false);

                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert2, normal2, uv2, intersection2, null, intersection2Uv, _useSharedVertices, false);
                    }
                    //Vert1 is alone
                    else
                    {
                        //Cast a ray from v1 to v2 and from v1 to v3 to get the intersections                       
                        intersection1 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert2, uv2, out intersection1Uv);
                        intersection2 = GetRayPlaneIntersectionPointAndUv(vert1, uv1, vert3, uv3, out intersection2Uv);

                        AddTrianglesNormalAndUvs(side1, vert1, normal1, uv1, intersection1, null, intersection1Uv, intersection2, null, intersection2Uv, _useSharedVertices, false);

                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert2, normal2, uv2, vert3, normal3, uv3, _useSharedVertices, false);
                        AddTrianglesNormalAndUvs(side2, intersection1, null, intersection1Uv, vert3, normal3, uv3, intersection2, null, intersection2Uv, _useSharedVertices, false);
                    }

                    if (!_edgesAlongPlane.Contains((intersection1, intersection2)))
                        _edgesAlongPlane.Add((intersection1, intersection2));

                    if (!_pointsAlongPlaneUVs.ContainsKey(intersection1))
                        _pointsAlongPlaneUVs.Add(intersection1, intersection1Uv);
                    if (!_pointsAlongPlaneUVs.ContainsKey(intersection2))
                        _pointsAlongPlaneUVs.Add(intersection2, intersection2Uv);
                }
            }

            //If the object is solid, join the new points along the plane otherwise do the reverse winding
            if (IsSolid)
            {
                JoinPointsAlongPlane();
            }

            else if (_createReverseTriangleWindings)
            {
                AddReverseTriangleWinding();
            }

            if (_smoothVertices)
            {
                SmoothVertices();
            }
        }

        /// <summary>
        /// Casts a ray from vertex1 to vertex2 and gets the point of intersection with the plan, calculates the new uv as well.
        /// </summary>
        /// <param name="vertex1">The vertex1.</param>
        /// <param name="vertex1Uv">The vertex1 uv.</param>
        /// <param name="vertex2">The vertex2.</param>
        /// <param name="vertex2Uv">The vertex2 uv.</param>
        /// <param name="uv">The uv.</param>
        /// <returns>Point of intersection</returns>
        private Vector3 GetRayPlaneIntersectionPointAndUv(Vector3 vertex1, Vector2 vertex1Uv, Vector3 vertex2, Vector2 vertex2Uv, out Vector2 uv)
        {
            var distance = GetDistanceRelativeToPlane(vertex1, vertex2, out var pointOfIntersection);
            uv = InterpolateUvs(vertex1Uv, vertex2Uv, distance);
            return pointOfIntersection;
        }

        /// <summary>
        /// Computes the distance based on the plane.
        /// </summary>
        /// <param name="vertex1">The vertex1.</param>
        /// <param name="vertex2">The vertex2.</param>
        /// <param name="pointOfIntersection">The point of intersection.</param>
        /// <returns></returns>
        private float GetDistanceRelativeToPlane(Vector3 vertex1, Vector3 vertex2, out Vector3 pointOfIntersection)
        {
            var ray = new Ray(vertex1, (vertex2 - vertex1));
            _plane.Raycast(ray, out var distance);
            pointOfIntersection = ray.GetPoint(distance);
            return distance;
        }

        /// <summary>
        /// Get a uv between the two provided uvs by the distance.
        /// </summary>
        /// <param name="uv1">The uv1.</param>
        /// <param name="uv2">The uv2.</param>
        /// <param name="distance">The distance.</param>
        /// <returns></returns>
        private static Vector2 InterpolateUvs(Vector2 uv1, Vector2 uv2, float distance)
        {
            var uv = Vector2.Lerp(uv1, uv2, distance);
            return uv;
        }

        /// <summary>
        /// Gets the point perpendicular to the face defined by the provided vertices        
        /// https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="vertex3"></param>
        /// <returns></returns>
        private static Vector3 ComputeNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            var side1 = vertex2 - vertex1;
            var side2 = vertex3 - vertex1;

            var normal = Vector3.Cross(side1, side2);

            return normal;
        }

        /// <summary>
        /// Reverse the normals in a given list
        /// </summary>
        /// <param name="currentNormals"></param>
        /// <returns></returns>
        private static IEnumerable<Vector3> FlipNormals(IEnumerable<Vector3> currentNormals) => currentNormals.Select(normal => -normal).ToList();

        private void SmoothVertices()
        {
            DoSmoothing(ref _positiveSideVertices, ref _positiveSideNormals, ref _positiveSideTriangles);
            DoSmoothing(ref _negativeSideVertices, ref _negativeSideNormals, ref _negativeSideTriangles);
        }

        private static void DoSmoothing(ref List<Vector3> vertices, ref List<Vector3> normals, ref List<int> triangles)
        {
            for (var index = 0; index < normals.Count; index++)
            {
                normals[index] = Vector3.zero;
            }

            for (var i = 0; i < triangles.Count; i += 3)
            {
                var vertIndex1 = triangles[i];
                var vertIndex2 = triangles[i + 1];
                var vertIndex3 = triangles[i + 2];

                var triangleNormal = ComputeNormal(vertices[vertIndex1], vertices[vertIndex2], vertices[vertIndex3]);

                normals[vertIndex1] += triangleNormal;
                normals[vertIndex2] += triangleNormal;
                normals[vertIndex3] += triangleNormal;
            }

            normals.ForEach(x => { x.Normalize(); });
        }
    }
}