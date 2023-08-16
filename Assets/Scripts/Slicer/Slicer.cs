using System.Collections.Generic;
using System.Linq;
using MeshSlicer.MeshCombining;
using UnityEngine;

namespace MeshSlicer.Slicer
{
    internal static class Slicer
    {
        /// <summary>
        /// Slice the object by the plane 
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="objectToCut"></param>
        /// <param name="sliceParameters"></param>
        /// <returns></returns>
        public static GameObject[] Slice(Plane plane, GameObject objectToCut, SliceableObjectParameters sliceParameters = null)
        {
            //Get the current mesh and its verts and tris
            var mesh = objectToCut.GetComponent<MeshFilter>().sharedMesh;

            if (sliceParameters == null)
                sliceParameters = objectToCut.GetComponent<SliceableObject>().SliceableObjectParameters;

            if (sliceParameters == null)
            {
                return null;
            }

            var positiveResultingSubMeshes = new List<(Mesh, int)>();
            var negativeResultingSubMeshes = new List<(Mesh, int)>();
            
            for (var i = 0; i < mesh.subMeshCount; i++)
            {
                var tempMesh = mesh.GetMeshDataFromSubMesh(i);
                
                var slicesMeta = new SlicesMetadata(plane, tempMesh, sliceParameters.IsSolid, sliceParameters.ReverseWireTriangles,
                    sliceParameters.ShareVertices, sliceParameters.SmoothVertices);
                slicesMeta.ComputeNewMeshes();
                
                positiveResultingSubMeshes.Add((slicesMeta.PositiveSideMesh, i));
                negativeResultingSubMeshes.Add((slicesMeta.NegativeSideMesh, i));
            }            
            //Create left and right slice of hollow object

            var positiveCombiners = positiveResultingSubMeshes.Select(x => new CombineInstance()
            {
                mesh = x.Item1,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            });

            var finalPositiveMesh = new Mesh();
            finalPositiveMesh.CombineMeshes(positiveCombiners.ToArray(), false);
            
            var negativeCombiners = negativeResultingSubMeshes.Select(x => new CombineInstance()
            {
                mesh = x.Item1,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            });

            var finalNegativeMesh = new Mesh();
            finalNegativeMesh.CombineMeshes(negativeCombiners.ToArray(), false);
            
            var positiveSubMeshesIds = positiveResultingSubMeshes.Where(x => x.Item1.triangles.Length > 0).Select(x => x.Item2).ToArray();
            var negativeSubMeshesIds = negativeResultingSubMeshes.Where(x => x.Item1.triangles.Length > 0).Select(x => x.Item2).ToArray();
            
            var positiveObject = CreateMeshGameObject(objectToCut, positiveSubMeshesIds, -finalPositiveMesh.bounds.center);
            positiveObject.name = $"{objectToCut.name}_positive";

            var negativeObject = CreateMeshGameObject(objectToCut, negativeSubMeshesIds, -finalNegativeMesh.bounds.center);
            negativeObject.name = $"{objectToCut.name}_negative";

            finalPositiveMesh.vertices = finalPositiveMesh.vertices.Select(vertex => vertex - finalPositiveMesh.bounds.center).ToArray();
            finalNegativeMesh.vertices = finalNegativeMesh.vertices.Select(vertex => vertex - finalNegativeMesh.bounds.center).ToArray();
            finalPositiveMesh.RecalculateBounds();
            finalNegativeMesh.RecalculateBounds();
            

            positiveObject.GetComponent<MeshFilter>().sharedMesh = finalPositiveMesh;
            negativeObject.GetComponent<MeshFilter>().sharedMesh = finalNegativeMesh;

            SetupCollidersAndRigidBodies(ref positiveObject, finalPositiveMesh, sliceParameters.UseGravity);
            SetupCollidersAndRigidBodies(ref negativeObject, finalNegativeMesh, sliceParameters.UseGravity, false);

            return new[] {positiveObject, negativeObject};
        }

        /// <summary>
        /// Creates the default mesh game object.
        /// </summary>
        /// <param name="originalObject">The original object.</param>
        /// <param name="subMeshesIds"></param>
        /// <param name="centerOffset"></param>
        /// <returns></returns>
        private static GameObject CreateMeshGameObject(GameObject originalObject, int[] subMeshesIds, Vector3 centerOffset)
        {
            var originalMaterial = originalObject.GetComponent<MeshRenderer>().materials;

            var meshGameObject = new GameObject();
            var originalSliceableObject = originalObject.GetComponent<SliceableObject>();

            meshGameObject.AddComponent<MeshFilter>();
            var meshRenderer = meshGameObject.AddComponent<MeshRenderer>();
            var sliceableObject = meshGameObject.AddComponent<SliceableObject>();
            originalSliceableObject.CopyPropertiesTo(sliceableObject);

            meshRenderer.materials = originalMaterial.Where((_, i) => subMeshesIds.Contains(i)).ToArray();

            meshGameObject.transform.localScale = originalObject.transform.localScale;
            meshGameObject.transform.rotation = originalObject.transform.rotation;
            meshGameObject.transform.position = originalObject.transform.position - centerOffset;

            meshGameObject.tag = originalObject.tag;

            return meshGameObject;
        }

        /// <summary>
        /// Add mesh collider and rigid body to game object
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="mesh"></param>
        /// <param name="useGravity"></param>
        /// <param name="addRigidBody"></param>
        private static void SetupCollidersAndRigidBodies(ref GameObject gameObject, Mesh mesh, bool useGravity, bool addRigidBody = true)
        {
            var meshCollider = gameObject.AddComponent<MeshCollider>();
            
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;
            meshCollider.isTrigger = true;

            if (!addRigidBody)
                return;

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = useGravity;
        }
    }
}