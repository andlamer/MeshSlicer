using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace MeshSlicer.CustomTriangulation
{
    public class Triangulation
    {
        public List<Vector3> GetTriangulationTriangles(List<(Vector3, Vector3)> points)
        {
            var triangulationPointsList = new List<Vector3>();

            foreach (var (firstPoint, secondPoint) in points)
            {
                if (!triangulationPointsList.Contains(firstPoint))
                    triangulationPointsList.Add(firstPoint);
                if (!triangulationPointsList.Contains(secondPoint))
                    triangulationPointsList.Add(secondPoint);
            }

            var pointsConverting = new PointsDimensionConverting(triangulationPointsList);

            var convertedEdges = points.Select(x => new Edge(
                    pointsConverting.Convert3DPointTo2DPointOnPlane(x.Item1),
                    pointsConverting.Convert3DPointTo2DPointOnPlane(x.Item2)))
                .ToArray();
            var polygonsData = GetWellBehavedTriangulationData(convertedEdges);

            var returnedTriangles = new List<Vector3>();

            foreach (var trianglesForPolygon in 
                     polygonsData
                         .Where(x => x.SortedOuterPoints is {Count: >= 3})
                         .Select(polygonData => GetTriangulationTriangles(polygonData.SortedOuterPoints.ToArray(), polygonData.SortedHoles)
                             .Select(point => pointsConverting.Convert2DPointTo3DPointWorldSpace(point))
                             .ToList())
                         .Where(trianglesForPolygon => trianglesForPolygon is {Count: > 0}))
            {
                returnedTriangles.AddRange(trianglesForPolygon);
            }

            return returnedTriangles;
        }

        private static List<TriangulationData> GetWellBehavedTriangulationData(Edge[] edges)
        {
            var triangulationDataList = new Dictionary<List<Vector2>, List<List<Vector2>>>();
            var viablePolygons = new List<List<Vector2>>();

            var edgesNum = edges.Length;

            while (edgesNum > 0)
            {
                var currentElement = edges[0];
                var tempPolygonList = new List<Vector2>
                {
                    currentElement.FirstPoint, currentElement.SecondPoint
                };

                edgesNum--;
                for (var k = 0; k < edgesNum; k++)
                {
                    edges[k] = edges[k + 1];
                }

                var index = 0;

                while (index < edgesNum)
                {
                    if (edges[index].HasPoint(tempPolygonList[^1], out var nextPoint))
                    {
                        // ReSharper disable once PossibleInvalidOperationException
                        tempPolygonList.Add(nextPoint.Value);
                        edgesNum--;

                        for (var k = index; k < edgesNum; k++)
                        {
                            edges[k] = edges[k + 1];
                        }

                        index = 0;
                    }
                    else
                    {
                        index++;
                    }
                }

                if (tempPolygonList[0] != tempPolygonList[^1]) continue;

                tempPolygonList.RemoveAt(tempPolygonList.Count - 1);

                if (TriangulationUtilities.IsClockwisePolygon(tempPolygonList))
                {
                    tempPolygonList.Reverse();
                }

                for (var i = 0; i < tempPolygonList.Count - 2; i++)
                {
                    if (TriangulationUtilities.CheckIfPointsAreCollinear(tempPolygonList[i],
                            tempPolygonList[i + 1], tempPolygonList[i + 2], out var collinearPoint))
                    {
                        // ReSharper disable once PossibleInvalidOperationException
                        tempPolygonList.Remove(collinearPoint.Value);
                    }
                }

                viablePolygons.Add(tempPolygonList);
            }

            if (viablePolygons.Count == 1)
            {
                return new List<TriangulationData>()
                {
                    new(viablePolygons[0], null)
                };
            }

            for (var i = 0; i < viablePolygons.Count; i++)
            {
                for (var j = i + 1; j < viablePolygons.Count; j++)
                {
                    if (!TriangulationUtilities.IsPolygonsLinesIntersect(viablePolygons[i], viablePolygons[j]))
                    {
                        if (TriangulationUtilities.IsPointInsidePolygon(viablePolygons[i], viablePolygons[j][0]))
                        {
                            if (!triangulationDataList.ContainsKey(viablePolygons[i]))
                            {
                                triangulationDataList.Add(viablePolygons[i], new List<List<Vector2>>()
                                {
                                    viablePolygons[j]
                                });
                            }
                            else
                            {
                                triangulationDataList[viablePolygons[i]] ??= new List<List<Vector2>>();
                                triangulationDataList[viablePolygons[i]].Add(viablePolygons[j]);
                            }
                        }
                        else if (TriangulationUtilities.IsPointInsidePolygon(viablePolygons[j], viablePolygons[i][0]))
                        {
                            if (!triangulationDataList.ContainsKey(viablePolygons[j]))
                            {
                                triangulationDataList.Add(viablePolygons[j], new List<List<Vector2>>()
                                {
                                    viablePolygons[i]
                                });
                            }
                            else
                            {
                                triangulationDataList[viablePolygons[j]] ??= new List<List<Vector2>>();
                                triangulationDataList[viablePolygons[j]].Add(viablePolygons[i]);
                            }
                        }
                        else
                        {
                            if (!triangulationDataList.ContainsKey(viablePolygons[i]))
                            {
                                triangulationDataList.Add(viablePolygons[i], null);
                            }

                            if (!triangulationDataList.ContainsKey(viablePolygons[j]))
                            {
                                triangulationDataList.Add(viablePolygons[j], null);
                            }
                        }
                    }
                    else
                    {
                        if (!triangulationDataList.ContainsKey(viablePolygons[i]))
                        {
                            triangulationDataList.Add(viablePolygons[i], null);
                        }

                        if (!triangulationDataList.ContainsKey(viablePolygons[j]))
                        {
                            triangulationDataList.Add(viablePolygons[j], null);
                        }
                    }
                }
            }

            return triangulationDataList.Select(x => new TriangulationData(x.Key, x.Value)).ToList();
        }

        private static IEnumerable<Vector2> GetTriangulationTriangles(Vector2[] outerPoints, List<List<Vector2>> holesPoints)
        {
            if (outerPoints.Length < 3)
            {
                return null;
            }

            // foreach (var outerPoint in outerPoints)
            // {
            //     var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //     sphere.transform.position = outerPoint;
            //     sphere.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            //     sphere.GetComponent<MeshRenderer>().material.color = Color.black;
            // }
            //
            // if (holesPoints is {Count: > 0})
            //     foreach (var hole in holesPoints)
            //     {
            //         foreach (var holePoint in hole)
            //         {
            //             var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //             sphere.transform.position = holePoint;
            //             sphere.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            //             sphere.GetComponent<MeshRenderer>().material.color = Color.white;
            //         }
            //     }

            var pointsToTriangulate = holesPoints is {Count: > 0}
                ? AddHolesToUnifiedPolygon(outerPoints, holesPoints)
                : outerPoints;

            var index = 0;
            var cornersNum = pointsToTriangulate.Length;
            var resultingTriangles = new List<Vector2>();

            while (index < cornersNum && cornersNum >= 3)
            {
                var j = (index + 1) % cornersNum;
                var k = (j + 1) % cornersNum;

                var triangleFound = false;

                if ((Math.Abs(pointsToTriangulate[index].x - pointsToTriangulate[j].x) > float.Epsilon ||
                     Math.Abs(pointsToTriangulate[index].y - pointsToTriangulate[j].y) > float.Epsilon) &&
                    (pointsToTriangulate[k].y - pointsToTriangulate[index].y) * (pointsToTriangulate[j].x - pointsToTriangulate[index].x) >=
                    (pointsToTriangulate[k].x - pointsToTriangulate[index].x) * (pointsToTriangulate[j].y - pointsToTriangulate[index].y))
                {
                    var anyPointsInsideTriangle = false;

                    int l;

                    for (l = 0; l < cornersNum; l++)
                    {
                        if (TriangulationUtilities.IsPointInsideTriangle(pointsToTriangulate[index], pointsToTriangulate[j],
                                pointsToTriangulate[k], pointsToTriangulate[l]))
                        {
                            anyPointsInsideTriangle = true;
                        }
                    }

                    if (!anyPointsInsideTriangle)
                    {
                        resultingTriangles.Add(pointsToTriangulate[index]);
                        resultingTriangles.Add(pointsToTriangulate[j]);
                        resultingTriangles.Add(pointsToTriangulate[k]);

                        cornersNum--;

                        for (l = j; l < cornersNum; l++)
                        {
                            pointsToTriangulate[l] = pointsToTriangulate[l + 1];
                        }

                        triangleFound = true;
                        index = 0;
                    }
                }

                if (!triangleFound)
                    index++;
            }

            if (cornersNum >= 3)
            {
                throw new Exception("Triangulation Failed");
            }

            return resultingTriangles;
        }

        private static Vector2[] AddHolesToUnifiedPolygon(IReadOnlyList<Vector2> outerPoints, List<List<Vector2>> holesSets)
        {
            var unifiedPoints = (Vector2[]) outerPoints;

            foreach (var holeSet in holesSets)
            {
                var minDistance = float.MaxValue;
                Vector2? minOuterPoint = null;
                Vector2? minHolePoint = null;

                foreach (var outerPoint in outerPoints)
                {
                    holeSet.Reverse();
                    foreach (var holePoint in holeSet)
                    {
                        var distanceBetweenPoints = TriangulationUtilities.DistanceBetweenTwoVectors(outerPoint, holePoint);

                        if (!(minDistance > distanceBetweenPoints)) continue;

                        var anyOuterIntersections = false;

                        for (var k = 0; k < outerPoints.Count; k++)
                        {
                            var l = (k + 1) % outerPoints.Count;

                            if (TriangulationUtilities.IsLineSegmentsIntersect(outerPoint, holePoint, outerPoints[k], outerPoints[l]))
                            {
                                anyOuterIntersections = true;
                            }
                        }

                        if (anyOuterIntersections) continue;

                        var anyHolesIntersections = false;

                        for (var k = 0; k < holeSet.Count; k++)
                        {
                            var l = (k + 1) % holeSet.Count;

                            if (TriangulationUtilities.IsLineSegmentsIntersect(outerPoint, holePoint, holeSet[k], holeSet[l]))
                            {
                                anyHolesIntersections = true;
                            }

                            if (anyHolesIntersections) continue;

                            minDistance = distanceBetweenPoints;
                            minOuterPoint = outerPoint;
                            minHolePoint = holePoint;
                        }
                    }
                }

                if (!minOuterPoint.HasValue)
                {
                    return null;
                }

                var prevUnifiedData = unifiedPoints;

                unifiedPoints = new Vector2[prevUnifiedData.Length + holeSet.Count + 2];

                var outsideIndex = prevUnifiedData.ToList().IndexOf(minOuterPoint.Value);
                var holeIndex = holeSet.ToList().IndexOf(minHolePoint.Value);

                for (var i = 0; i <= prevUnifiedData.Length; i++)
                {
                    unifiedPoints[i] = prevUnifiedData[(outsideIndex + i) % prevUnifiedData.Length];
                }

                for (var i = 0; i <= holeSet.Count; i++)
                {
                    unifiedPoints[prevUnifiedData.Length + 1 + i] = holeSet[(holeIndex + holeSet.Count - i) % holeSet.Count];
                }
            }

            return unifiedPoints;
        }
    }
}