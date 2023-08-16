using UnityEngine;

namespace MeshSlicer.Slicer
{
    public class SliceableObject : MonoBehaviour
    {
        [SerializeField] private SliceableObjectParameters sliceableObjectParameters;
        [SerializeField] private BendingParameters bendingParameters;

        public SliceableObjectParameters SliceableObjectParameters
        {
            get => sliceableObjectParameters;
            private set => sliceableObjectParameters = value;
        }

        public BendingParameters BendingParameters
        {
            get => bendingParameters;
            private set => bendingParameters = value;
        }
        
        public void CopyPropertiesTo(SliceableObject otherSliceable)
        {
            otherSliceable.SliceableObjectParameters = SliceableObjectParameters;
            otherSliceable.BendingParameters = BendingParameters;
        }
    }
}