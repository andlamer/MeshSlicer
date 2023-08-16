using Newtonsoft.Json;
using UnityEngine;

namespace MeshSlicer.CustomTriangulation
{
    public class TrianglePoint : MonoBehaviour
    {
        public Vector3 normal;

        public void OnMouseDown()
        {
            Debug.Log(JsonConvert.SerializeObject(normal));
        }
    }
}