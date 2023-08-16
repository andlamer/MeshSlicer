using MeshSlicer.MeshCombining;
using UnityEditor;
using UnityEngine;

namespace MeshSlicer.Editor.MeshCombinig
{
    [CustomEditor(typeof(MeshCombiner))]
    public class MeshCombinerEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var mc = target as MeshCombiner;

            if (Handles.Button(mc.transform.position + Vector3.up * 4, Quaternion.LookRotation(Vector3.up),
                    0.5f, 1, Handles.CylinderHandleCap))
            {
                mc.CombineMeshes();
            }
        }
    }
}