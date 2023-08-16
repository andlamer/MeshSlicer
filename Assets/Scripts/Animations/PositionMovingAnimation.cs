using System;
using DG.Tweening;
using UnityEngine;

public class PositionMovingAnimation : MonoBehaviour
{
    [SerializeField] private Transform objectToMove;
    [SerializeField] private Transform startingPoint;
    [SerializeField] private Transform targetPoint;

    [SerializeField] private float speed;
    [SerializeField] private bool resetOnStart = true;

    public event Action AnimationFinished;
    private Tweener _cachedTweener;
    
    // Start is called before the first frame update
    private void Start()
    {
        if (resetOnStart)
        {
            objectToMove.position = startingPoint.position;
        }
    }

    private void OnDestroy()
    {
        _cachedTweener.Kill();
    }

    [ContextMenu("Init and play")]
    public void InitializeAndPlay()
    {
        _cachedTweener = objectToMove
            .DOMove(targetPoint.position, speed)
            .SetSpeedBased(true)
            .OnComplete(() =>
            {
                AnimationFinished?.Invoke();
            });
    }

    public void Continue() => _cachedTweener.Play();
    public void Pause() => _cachedTweener.Pause();
}
