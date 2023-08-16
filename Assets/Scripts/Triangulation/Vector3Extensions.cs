using System;
using UnityEngine;

namespace MeshSlicer.CustomTriangulation
{
    public static class Vector3Extensions
    {
        public static bool Equals(this Vector3 vector, Vector3 otherVector, float comparisonEpsilon = 0.000001f) =>
            Math.Abs(vector.x - otherVector.x) < comparisonEpsilon &&
            Math.Abs(vector.y - otherVector.y) < comparisonEpsilon &&
            Math.Abs(vector.z - otherVector.z) < comparisonEpsilon;
    }
}