using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace MeshSlicer.CustomTriangulation
{
    public static class TriangulationUtilities
    {
        public static bool CheckIfPointsAreCollinear(Vector3 pointA, Vector3 pointB, Vector3 pointC, out Vector3? collinearPoint)
        {
            var distanceAB = DistanceBetweenTwoVectors(pointA, pointB);
            var distanceBC = DistanceBetweenTwoVectors(pointB, pointC);
            var distanceAC = DistanceBetweenTwoVectors(pointA, pointC);

            collinearPoint = null;
            if (Mathf.Approximately(distanceAB + distanceBC, distanceAC))
                collinearPoint = pointB;
            else if (Mathf.Approximately(distanceAB + distanceAC, distanceBC))
                collinearPoint = pointA;
            else if (Mathf.Approximately(distanceBC + distanceAC, distanceAB))
                collinearPoint = pointC;

            return collinearPoint.HasValue;
        }

        public static float DistanceBetweenTwoVectors(Vector3 pointA, Vector3 pointB)
        {
            return Mathf.Sqrt(Mathf.Pow(pointA.x - pointB.x, 2) +
                              Mathf.Pow(pointA.y - pointB.y, 2) +
                              Mathf.Pow(pointA.z - pointB.z, 2));
        }

        public static bool IsPointInsideTriangle(Vector2 trianglePointA, Vector2 trianglePointB, Vector2 trianglePointC, Vector2 pointToCheck)
        {
            if (pointToCheck == trianglePointA || pointToCheck == trianglePointB || pointToCheck == trianglePointC)
                return false;

            var isInside = false;

            if ((trianglePointA.y < pointToCheck.y && trianglePointB.y >= pointToCheck.y ||
                 trianglePointB.y < pointToCheck.y && trianglePointA.y >= pointToCheck.y) &&
                trianglePointA.x + (pointToCheck.y - trianglePointA.y) / (trianglePointB.y - trianglePointA.y) *
                (trianglePointB.x - trianglePointA.x) < pointToCheck.x)
            {
                isInside = !isInside;
            }

            if ((trianglePointB.y < pointToCheck.y && trianglePointC.y >= pointToCheck.y ||
                 trianglePointC.y < pointToCheck.y && trianglePointB.y >= pointToCheck.y) &&
                trianglePointB.x + (pointToCheck.y - trianglePointB.y) / (trianglePointC.y - trianglePointB.y) *
                (trianglePointC.x - trianglePointB.x) < pointToCheck.x)
            {
                isInside = !isInside;
            }

            if ((trianglePointC.y < pointToCheck.y && trianglePointA.y >= pointToCheck.y ||
                 trianglePointA.y < pointToCheck.y && trianglePointC.y >= pointToCheck.y) &&
                trianglePointC.x + (pointToCheck.y - trianglePointC.y) / (trianglePointA.y - trianglePointC.y) *
                (trianglePointA.x - trianglePointC.x) < pointToCheck.x)
            {
                isInside = !isInside;
            }

            return isInside;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static bool IsLineSegmentsIntersect(Vector2 pointA, Vector2 pointB, Vector2 pointC, Vector2 pointD)
        {
            if ((Math.Abs(pointA.x - pointB.x) < float.Epsilon && Math.Abs(pointA.y - pointB.y) < float.Epsilon) ||
                (Math.Abs(pointA.x - pointC.x) < float.Epsilon && Math.Abs(pointA.y - pointC.y) < float.Epsilon) ||
                (Math.Abs(pointA.x - pointD.x) < float.Epsilon && Math.Abs(pointA.y - pointD.y) < float.Epsilon) ||
                (Math.Abs(pointB.x - pointC.x) < float.Epsilon && Math.Abs(pointB.y - pointC.y) < float.Epsilon) ||
                (Math.Abs(pointB.x - pointD.x) < float.Epsilon && Math.Abs(pointB.y - pointD.y) < float.Epsilon) ||
                (Math.Abs(pointC.x - pointD.x) < float.Epsilon && Math.Abs(pointC.y - pointD.y) < float.Epsilon))
            {
                return false;
            }

            pointB -= pointA;
            pointC -= pointA;
            pointD -= pointA;

            var distanceAB = Mathf.Sqrt(Mathf.Pow(pointB.x, 2) + Mathf.Pow(pointB.y, 2));

            var cosAB = pointB.x / distanceAB;
            var sinAB = pointB.y / distanceAB;

            var newX = pointC.x * cosAB + pointC.y * sinAB;
            pointC.y = pointC.y * cosAB - pointC.x * sinAB;
            pointC.x = newX;

            newX = pointD.x * cosAB + pointD.y * sinAB;
            pointD.y = pointD.y * cosAB - pointC.x * sinAB;
            pointD.x = newX;

            if (pointC.y < 0 && pointD.y < 0 ||
                pointC.y > 0 && pointD.y > 0)
            {
                return false;
            }

            var intersectionPositionOnAB = pointD.x + (pointC.x - pointD.x) * pointD.y / (pointD.y - pointC.y);

            var anyIntersections = intersectionPositionOnAB > 0 && intersectionPositionOnAB < distanceAB;

            if (anyIntersections)
            {
                Debug.DrawLine(pointA, pointB, Color.red, 15f);
                Debug.DrawLine(pointC, pointD, Color.green, 15f);
            }

            return anyIntersections;
        }

        public static bool IsPointInsidePolygon(List<Vector2> polygon, Vector2 testPoint)
        {
            var result = false;
            var j = polygon.Count - 1;

            for (var i = 0; i < polygon.Count; i++)
            {
                if (polygon[i].y < testPoint.y && polygon[j].y >= testPoint.y ||
                    polygon[j].y < testPoint.y && polygon[i].y >= testPoint.y)
                {
                    if (polygon[i].x + (testPoint.y - polygon[i].y) /
                        (polygon[j].y - polygon[i].y) *
                        (polygon[j].x - polygon[i].x) < testPoint.x)
                    {
                        result = !result;
                    }
                }

                j = i;
            }

            return result;
        }

        public static bool IsPolygonsLinesIntersect(List<Vector2> firstPolygon, List<Vector2> secondPolygon)
        {
            for (var i = 0; i < firstPolygon.Count - 1; i++)
            {
                for (var j = 0; j < secondPolygon.Count - 1; j++)
                {
                    if (IsLineSegmentsIntersect(firstPolygon[i], firstPolygon[i + 1],
                            secondPolygon[j], secondPolygon[j + 1]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsClockwisePolygon(List<Vector2> polygon)
        {
            double sum = 0;
            
            for (var i = 0; i < polygon.Count - 1; i++)
            {
                sum += (polygon[i + 1].x - polygon[i].x) * (polygon[i + 1].y + polygon[i].y);
            }

            sum += (polygon[0].x - polygon[^1].x) * (polygon[0].y + polygon[^1].y);
            
            return sum > 0;;
        }
    }
}