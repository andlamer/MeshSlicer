using UnityEngine;

namespace MeshSlicer.Slicer
{
    [CreateAssetMenu(menuName = "Scriptable objects/Create BendingParameters", fileName = "BendingParameters", order = 0)]
    public class BendingParameters : ScriptableObject
    {
        [SerializeField] private Vector2 sizeClampBoundaries = new(0.01f, 1f);
        [SerializeField] private Vector2 deviationModificationBoundaries = new(3.5f, 0.01f);
        [SerializeField] private Vector2 radiusModificationBoundaries = new(1.2f, 1f);
        [SerializeField] private AnimationCurve deviationCurve;
        [SerializeField] private AnimationCurve radiusCurve;

        public Vector2 SizeClampBoundaries => sizeClampBoundaries;
        public Vector2 DeviationModificationBoundaries => deviationModificationBoundaries;
        public Vector2 RadiusModificationBoundaries => radiusModificationBoundaries;
        public AnimationCurve DeviationCurve => deviationCurve;
        public AnimationCurve RadiusCurve => radiusCurve;
    }
}