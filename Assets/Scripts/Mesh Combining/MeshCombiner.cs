using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeshSlicer.MeshCombining
{
    public class MeshCombiner : MonoBehaviour
    {
        [SerializeField] private MeshFilter targetFilter;
        [SerializeField] private MeshRenderer targetMeshRenderer;
        [SerializeField] private MeshCollider targetMeshCollider;

        public void CombineMeshes()
        {
            var cachedTransform = transform;

            var cachedRotation = cachedTransform.rotation;
            var cachedPosition = cachedTransform.position;

            cachedTransform.rotation = Quaternion.identity;
            cachedTransform.position = Vector3.zero;

            var meshRenderers = GetComponentsInChildren<MeshRenderer>(false);
            var meshFilters = GetComponentsInChildren<MeshFilter>();

            var combinedMaterials = new List<Material>();
            var subMeshes = new List<Mesh>();

            foreach (var meshRenderer in meshRenderers)
            {
                if (meshRenderer.transform == targetMeshRenderer.transform)
                {
                    continue;
                }

                var tempMaterials = meshRenderer.sharedMaterials;

                foreach (var material in tempMaterials)
                {
                    if (!combinedMaterials.Contains(material))
                    {
                        combinedMaterials.Add(material);
                    }
                }
            }

            foreach (var material in combinedMaterials)
            {
                var combineInstances = new List<CombineInstance>();

                foreach (var filter in meshFilters)
                {
                    var meshRenderer = filter.GetComponent<MeshRenderer>();

                    if (meshRenderer == null)
                    {
                        continue;
                    }

                    var localMaterials = meshRenderer.sharedMaterials;

                    for (var i = 0; i < localMaterials.Length; i++)
                    {
                        if (localMaterials[i] != material)
                        {
                            continue;
                        }

                        var combineInstance = new CombineInstance
                        {
                            mesh = filter.sharedMesh,
                            subMeshIndex = i,
                            transform = filter.transform.localToWorldMatrix
                        };
                        combineInstances.Add(combineInstance);
                    }
                }

                var mesh = new Mesh();
                mesh.CombineMeshes(combineInstances.ToArray(), true);
                subMeshes.Add(mesh);
            }

            var finalCombiners = subMeshes
                .Select(subMesh => new CombineInstance
                {
                    mesh = subMesh,
                    subMeshIndex = 0,
                    transform = Matrix4x4.identity
                })
                .ToList();

            var resultingMesh = new Mesh();
            resultingMesh.CombineMeshes(finalCombiners.ToArray(), false);

            targetFilter.sharedMesh = resultingMesh;
            targetMeshCollider.sharedMesh = resultingMesh;

            targetMeshRenderer.sharedMaterials = combinedMaterials.ToArray();

            cachedTransform.rotation = cachedRotation;
            cachedTransform.position = cachedPosition;

            for (var i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);
        }
    }
}