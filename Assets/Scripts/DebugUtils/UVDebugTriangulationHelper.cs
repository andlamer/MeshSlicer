using MeshSlicer.CustomTriangulation;
using UnityEngine;

namespace MeshSlicer.DebugUtils
{
    public class UVDebugTriangulationHelper : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;

        [ContextMenu("Spawn debug points")]
        public void SpawnDebugPoints()
        {
            var parent = meshFilter.gameObject;

            for (var index = 0; index < meshFilter.sharedMesh.vertices.Length; index++)
            {
                var vertex = meshFilter.sharedMesh.vertices[index];
                var tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tempPrimitive.transform.SetParent(parent.transform, false);
                tempPrimitive.transform.localPosition = vertex;
                tempPrimitive.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                var pointComponent = tempPrimitive.AddComponent<TrianglePoint>();
                pointComponent.normal = meshFilter.sharedMesh.normals[index];
            }
        }
    }
}