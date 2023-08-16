using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace MeshSlicer.Slicer
{
    public class SlicerObject : MonoBehaviour
    {
        [SerializeField] [Tooltip("The empty game object located at the tip of the blade")]
        private Transform tipOfBlade;

        [SerializeField] [Tooltip("The empty game object located at the base of the blade")]
        private Transform baseOfBlade;

        [SerializeField] [Tooltip("The empty game object located at the top center of the blade")]
        private Transform topCenterOfBlade;


        [SerializeField] private Transform slicerTransform;
        [SerializeField] private float minYPosition = 0f;
        [SerializeField] private float maxYPosition;
        [SerializeField] private float loweringSpeed;
        [SerializeField] private float resetSpeed;
        [SerializeField] private float cooldown;

        public event Action SliceWasFinished;

        private bool _canSlice = true;
        private bool _sliceInProgress;
        private bool _cooldownInProgress;

        private float _previousYPosition;

        private void Awake()
        {
            _previousYPosition = tipOfBlade.position.y;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_canSlice)
                return;

            var othersGameObject = other.gameObject;
            if (!othersGameObject.TryGetComponent<SliceableObject>(out var othersSliceable))
            {
                return;
            }

            try
            {
                //Create a triangle between the tip and base so that we can get the normal
                var side1 = baseOfBlade.position - tipOfBlade.position;
                var side2 = baseOfBlade.position - topCenterOfBlade.position;

                //Get the point perpendicular to the triangle above which is the normal
                var normal = Vector3.Cross(side1, side2).normalized;

                //Transform the normal so that it is aligned with the object we are slicing transform.
                var transformedNormal = ((Vector3) (othersGameObject.transform.localToWorldMatrix.transpose * normal)).normalized;

                //Get the enter position relative to the object we're cutting's local transform
                var transformedStartingPoint = othersGameObject.transform.InverseTransformPoint(tipOfBlade.position);

                SliceManager.Instance.SliceObject(othersGameObject, othersSliceable, transformedNormal, transformedStartingPoint);
                _sliceInProgress = true;
                _canSlice = false;
            }
            catch (Exception e) when(e.Message.Contains("Triangulation Failed"))
            {
                ResetHeightToStartingPosition(true);
            }
        }

        public void MoveDown()
        {
            if (_cooldownInProgress)
                return;

            slicerTransform.DOBlendableMoveBy(new Vector3(0, -loweringSpeed * Time.deltaTime, 0), Time.deltaTime);

            if (!_sliceInProgress || tipOfBlade.position.y > _previousYPosition ) return;

            _previousYPosition = tipOfBlade.position.y;

            if (_previousYPosition <= minYPosition)
            {
                _sliceInProgress = false;
                SliceManager.Instance.OnSliceFinished();
                ResetHeightToStartingPosition(true);
            }
            else
            {
                SliceManager.Instance.OnSlicerMove(tipOfBlade.position.y);
            }
        }

        public void ResetHeight()
        {
            ResetHeightToStartingPosition(!_sliceInProgress, false);
        }
        
        private void ResetHeightToStartingPosition(bool sliceFinished = false, bool waitForCooldown = true)
        {
            slicerTransform
                .DOMoveY(maxYPosition, resetSpeed)
                .SetSpeedBased(true)
                .OnComplete(() =>
                {
                    if (!sliceFinished) return;

                    SliceWasFinished?.Invoke();

                    if (waitForCooldown)
                    {
                        StartCoroutine(nameof(WaitForCooldown));
                    }
                    else
                    {
                        _canSlice = true;
                        _previousYPosition = tipOfBlade.position.y;
                    }
                });
        }

        private IEnumerator WaitForCooldown()
        {
            _cooldownInProgress = true;

            yield return new WaitForSeconds(cooldown);

            _cooldownInProgress = false;
            _canSlice = true;
            _previousYPosition = tipOfBlade.position.y;
        }
    }
}