using System;
using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine;

namespace MeshSlicer.MeshBending
{
    public class MeshBending
    {
        private float _pointY;

        private const float UnityPI = 3.14159265359f;

        private readonly Vector3[] _vertices;
        private readonly Vector3 _upDirection;
        private readonly Vector3 _rollDirection;
        private readonly float _radius;
        private readonly float _deviation;
        private readonly float _meshTop;
        private readonly float _pointX;

        public MeshBending(Vector3[] vertices, float pointX, Vector3 upDirection, Vector3 rollDirection, float radius, float deviation, float meshTop)
        {
            _vertices = vertices;
            _pointX = pointX;
            _upDirection = upDirection;
            _rollDirection = rollDirection;
            _radius = radius;
            _deviation = deviation;
            _meshTop = meshTop;
        }

        public Vector3[] BendMesh(float pointY)
        {
            var resultingVertices = new Vector3[_vertices.Length];

            _pointY = pointY;

            for (var index = 0; index < _vertices.Length; index++)
            {
                resultingVertices[index] = BendMeshPoint(_vertices[index]);
            }

            return resultingVertices;
        }

        private Vector3 BendMeshPoint(Vector3 point)
        {
            var v0 = point;

            var upDir = Vector3.Normalize(_upDirection);
            var rollDir = Vector3.Normalize(_rollDirection);

            //float y = UNITY_ACCESS_INSTANCED_PROP(Props, _PointY);
            var y = _pointY;

            var dP = Vector3.Dot(v0 - upDir * y, upDir);

            dP = Math.Max(0, dP);

            var fromInitialPos = upDir * dP;
            v0 -= fromInitialPos;

            var radius = _radius + _deviation * Math.Max(0, -(y - _meshTop));
            var length = 2 * UnityPI * (radius - _deviation * Math.Max(0, -(y - _meshTop)) / 2);
            var r = dP / Math.Max(0, length);
            var a = 2 * r * UnityPI;

            var s = (float) Math.Sin(a);
            var c = (float) Math.Cos(a);

            var oneMinusC = 1f - c;

            var axis = Vector3.Normalize(Vector3.Cross(upDir, rollDir));

            var rotationMatrix = DenseMatrix.OfArray(new double[,]
                {
                    {oneMinusC * axis.x * axis.x + c, oneMinusC * axis.x * axis.y - axis.z * s, oneMinusC * axis.z * axis.x + axis.y * s},
                    {oneMinusC * axis.x * axis.y + axis.z * s, oneMinusC * axis.y * axis.y + c, oneMinusC * axis.y * axis.z - axis.x * s},
                    {oneMinusC * axis.z * axis.x - axis.y * s, oneMinusC * axis.y * axis.z + axis.x * s, oneMinusC * axis.z * axis.z + c}
                }
            );

            var cycleCenter = rollDir * _pointX + rollDir * radius + upDir * y;
            var cycleCenterDenseMatrix = DenseMatrix.OfArray(new double[,] {{cycleCenter.x}, {cycleCenter.y}, {cycleCenter.z}});

            var fromCenter = v0 - cycleCenter;
            var shiftFromCenterAxis = Vector3.Cross(axis, fromCenter);
            shiftFromCenterAxis = Vector3.Cross(shiftFromCenterAxis, axis);
            shiftFromCenterAxis = Vector3.Normalize(shiftFromCenterAxis);
            fromCenter -= shiftFromCenterAxis * _deviation * dP;
            
            var fromCenterDenseMatrix = DenseMatrix.OfArray(new double[,] {{fromCenter.x}, {fromCenter.y}, {fromCenter.z}});

            var resultingVector = rotationMatrix * fromCenterDenseMatrix + cycleCenterDenseMatrix;

            v0 = new Vector3((float) resultingVector.Values[0], (float) resultingVector.Values[1], (float) resultingVector.Values[2]);
            
            return v0;
        }
    }
}