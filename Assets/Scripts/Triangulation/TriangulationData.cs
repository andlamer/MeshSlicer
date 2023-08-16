using System.Collections.Generic;
using UnityEngine;

namespace MeshSlicer.CustomTriangulation
{
    public class TriangulationData
    {
        public List<Vector2> SortedOuterPoints;
        public List<List<Vector2>> SortedHoles;

        public TriangulationData(List<Vector2> outerPoints, List<List<Vector2>> holesPoints)
        {
            SortedOuterPoints = outerPoints;
            SortedHoles = holesPoints;
        }
    }
}