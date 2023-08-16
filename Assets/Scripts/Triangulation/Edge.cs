using UnityEngine;

namespace MeshSlicer.CustomTriangulation
{
    public struct Edge
    {
        public Vector2 FirstPoint;
        public Vector2 SecondPoint;

        public Edge(Vector2 pointA, Vector2 pointB)
        {
            FirstPoint = pointA;
            SecondPoint = pointB;
        }

        public bool HasPoint(Vector2 point, out Vector2? otherPoint)
        {
            otherPoint = FirstPoint == point ? SecondPoint
                : SecondPoint == point ? FirstPoint
                : null;

            return otherPoint.HasValue;
        }
    }
}