using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace MeshSlicer.CustomTriangulation
{
    public class PointsDimensionConverting
    {
        private Matrix4x4 _matrix3DTo2D;

        private readonly Dictionary<Vector3, Vector2> _converted3DTo2DPoints;

        public PointsDimensionConverting(IReadOnlyList<Vector3> basePoints)
        {
            _converted3DTo2DPoints = new Dictionary<Vector3, Vector2>();
            
            GetTransformationMatricesForPointsConverting(basePoints);

            foreach (var point in basePoints)
            {
                if (!_converted3DTo2DPoints.ContainsKey(point))
                {
                    _converted3DTo2DPoints.Add(point, _matrix3DTo2D.MultiplyPoint3x4(point));
                }
            }
        }

        public Vector3 Convert2DPointTo3DPointWorldSpace(Vector2 point)
        {
            foreach (var entry in _converted3DTo2DPoints.Where(entry => entry.Value == point))
            {
                return entry.Key;
            }

            throw new ArgumentOutOfRangeException();
        }

        public Vector2 Convert3DPointTo2DPointOnPlane(Vector3 point)
        {
            foreach (var entry in _converted3DTo2DPoints.Where(entry => entry.Key == point))
            {
                return entry.Value;
            }

            throw new ArgumentOutOfRangeException();
        }

        private void GetTransformationMatricesForPointsConverting(IReadOnlyList<Vector3> points)
        {
            if (!TryFindNonCollinearPointsForConvertingPlane(points, out var pointA, out var pointB, out var pointC))
            {
                throw new Exception("Failed to get non collinear points");
            }

            // ReSharper disable once InconsistentNaming
            var vectorAB = pointB - pointA;
            // ReSharper disable once InconsistentNaming
            var vectorAC = pointC - pointA;

            var normalToPlane = Vector3.Cross(vectorAB, vectorAC);

            // ReSharper disable once InconsistentNaming
            var normalizedAB = vectorAB.normalized;
            var normalizedNormal = normalToPlane.normalized;

            var secondBaseVector = Vector3.Cross(normalizedAB, normalizedNormal);

            var firstBasisPoint = pointA;
            var secondBasisPoint = pointA + normalizedAB;
            var thirdBasisPoint = pointA + secondBaseVector;
            var fourthBasisPoint = pointA + normalizedNormal;

            var matrixS = new Matrix4x4(
                new Vector4(firstBasisPoint.x, firstBasisPoint.y, firstBasisPoint.z, 1),
                new Vector4(secondBasisPoint.x, secondBasisPoint.y, secondBasisPoint.z, 1),
                new Vector4(thirdBasisPoint.x, thirdBasisPoint.y, thirdBasisPoint.z, 1),
                new Vector4(fourthBasisPoint.x, fourthBasisPoint.y, fourthBasisPoint.z, 1));

            var matrixD = new Matrix4x4(
                new Vector4(0, 0, 0, 1),
                new Vector4(1, 0, 0, 1),
                new Vector4(0, 1, 0, 1),
                new Vector4(0, 0, 1, 1));

            _matrix3DTo2D = matrixD * matrixS.inverse;
        }

        private static bool TryFindNonCollinearPointsForConvertingPlane(IReadOnlyList<Vector3> points, out Vector3 firstPoint,
            out Vector3 secondPoint, out Vector3 thirdPoint)
        {
            firstPoint = Vector3.zero;
            secondPoint = Vector3.zero;
            thirdPoint = Vector3.zero;

            for (var i = 0; i < points.Count; i++)
            {
                for (var j = i + 1; j < points.Count; j++)
                {
                    for (var k = j + 1; k < points.Count; k++)
                    {
                        if (points[i].Equals(points[k], 0.01f) ||
                            points[j].Equals(points[k], 0.01f) ||
                            points[i].Equals(points[j], 0.01f)) continue;

                        if (TriangulationUtilities.CheckIfPointsAreCollinear(points[i], points[j], points[k], out _)) continue;

                        firstPoint = points[i];
                        secondPoint = points[j];
                        thirdPoint = points[k];

                        return true;
                    }
                }
            }

            return false;
        }
    }
}