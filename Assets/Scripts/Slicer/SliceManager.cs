using System.Collections;
using UnityEngine;

namespace MeshSlicer.Slicer
{
    public class SliceManager : MonoBehaviour
    {
        public static SliceManager Instance;

        [SerializeField] private float forceAppliedToPositiveCut = 0.01f;
        [SerializeField] private float autoDestroyTime = 5f;
        [SerializeField] private Transform movingPlatform;
        [SerializeField] private Material tessMaterial;

        public int TotalSlicesCount { get; private set; }

        private MeshBending.MeshBending _meshBending;
        private Mesh _cachedMesh;
        private Rigidbody _rigidbody;
        private Vector3 _forceDirection;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        public void SliceObject(GameObject objectToSlice, SliceableObject sliceableComponent, Vector3 normal, Vector3 startingPoint)
        {
            var plane = new Plane();
            plane.SetNormalAndPosition(normal, startingPoint);

            var direction = Vector3.Dot(Vector3.up, startingPoint);

            //Flip the plane so that we always know which side the positive mesh is on
            plane = direction < 0 ? plane.flipped : plane;

            var slices = Slicer.Slice(plane, objectToSlice.gameObject, sliceableComponent.SliceableObjectParameters);

            if (slices == null)
            {
                return;
            }

            Destroy(objectToSlice.gameObject);

            _rigidbody = slices[0].GetComponent<Rigidbody>();
            var positiveMeshFilter = slices[0].GetComponent<MeshFilter>();

            SetUpMeshBending(sliceableComponent.BendingParameters, positiveMeshFilter.sharedMesh, slices[0], plane);

            _cachedMesh = positiveMeshFilter.sharedMesh;

            _forceDirection = normal + Vector3.up * forceAppliedToPositiveCut;
            _rigidbody.isKinematic = true;

            slices[1].transform.SetParent(movingPlatform);
        }

        public void OnSlicerMove(float newYValue)
        {
            _cachedMesh.vertices = _meshBending?.BendMesh(newYValue - newYValue / 2);
        }

        public void OnSliceFinished()
        {
            _rigidbody.isKinematic = false;
            _rigidbody.AddForce(_forceDirection, ForceMode.Impulse);
            StartCoroutine(nameof(SetObjectDestroyTimer), _rigidbody.gameObject);
            TotalSlicesCount++;

            _rigidbody = null;
            _cachedMesh = null;
            _forceDirection = Vector3.zero;
            _meshBending = null;
        }

        private void SetUpMeshBending(BendingParameters bendingParameters, Mesh mesh, GameObject parentGameObject, Plane plane)
        {
            var clampedSize = Mathf.Clamp(mesh.bounds.size.z, bendingParameters.SizeClampBoundaries.x,
                bendingParameters.SizeClampBoundaries.y) / bendingParameters.SizeClampBoundaries.y;

            var radius = clampedSize * Mathf.Lerp(bendingParameters.RadiusModificationBoundaries.x,
                bendingParameters.RadiusModificationBoundaries.y, bendingParameters.RadiusCurve.Evaluate(clampedSize));

            var deviation = Mathf.Lerp(bendingParameters.DeviationModificationBoundaries.x,
                bendingParameters.DeviationModificationBoundaries.y, bendingParameters.DeviationCurve.Evaluate(clampedSize));

            var pointX = mesh.bounds.size.z / 2;
            var meshTop = mesh.bounds.size.y;

            _meshBending = new MeshBending.MeshBending(mesh.vertices, pointX, parentGameObject.transform.up, plane.normal, radius, deviation, meshTop);
        }

        private IEnumerator SetObjectDestroyTimer(GameObject gameObjectToDestroy)
        {
            yield return new WaitForSeconds(autoDestroyTime);

            Destroy(gameObjectToDestroy);
        }
    }
}