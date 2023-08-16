using System;
using MeshSlicer.Slicer;
using UnityEngine;

namespace MeshSlicer.Main
{
    public class GameFlowManager : MonoBehaviour
    {
        [SerializeField] private PositionMovingAnimation platformMovingAnimation;
        [SerializeField] private SlicerObject slicerObject;

        public event Action OnGameFinish;

        private bool _inputBlocked = true;
        private bool _isTouchInProgress;

        private void Start()
        {
            slicerObject.SliceWasFinished += OnSliceFinished;
            platformMovingAnimation.AnimationFinished += OnAnimationFinished;
        }

        private void Update()
        {
            if (_inputBlocked) return;

            if (Input.GetMouseButtonDown(0))
            {
                OnTouchStart();
            }

            if (Input.GetMouseButton(0))
            {
                OnTouchInProgress();
            }

            if (Input.GetMouseButtonUp(0))
            {
                OnTouchFinished();
            }
        }

        public void StartGame()
        {
            platformMovingAnimation.InitializeAndPlay();
            _inputBlocked = false;
        }

        private void OnTouchStart()
        {
            platformMovingAnimation.Pause();
        }

        private void OnTouchInProgress()
        {
            slicerObject.MoveDown();
        }

        private void OnTouchFinished()
        {
            slicerObject.ResetHeight();
        }

        private void OnSliceFinished()
        {
            platformMovingAnimation.Continue();
        }

        private void OnAnimationFinished()
        {
            _inputBlocked = true;
            OnGameFinish?.Invoke();
        }
    }
}