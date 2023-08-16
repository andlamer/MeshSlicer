using UnityEngine;

namespace MeshSlicer.Slicer
{
    [CreateAssetMenu(menuName = "Scriptable objects/Create SliceableObjectParameters", fileName = "SliceableObjectParameters", order = 0)]
    public class SliceableObjectParameters : ScriptableObject
    {
        [SerializeField] private bool isSolid = true;

        [SerializeField] private bool reverseWindTriangles;

        [SerializeField] private bool useGravity;

        [SerializeField] private bool shareVertices;

        [SerializeField] private bool smoothVertices;

        public bool IsSolid => isSolid;

        public bool ReverseWireTriangles => reverseWindTriangles;

        public bool UseGravity => useGravity;

        public bool ShareVertices => shareVertices;


        public bool SmoothVertices => smoothVertices;
    }
}