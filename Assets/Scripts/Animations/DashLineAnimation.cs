using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DashLineAnimation : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private DecalProjector decalProjector;

    private Vector2 _previousOffset;

    // Update is called once per frame
    private void Update()
    {
        _previousOffset = decalProjector.uvBias;

        if (_previousOffset.x >= float.MaxValue - 1f)
        {
            _previousOffset = new Vector2(0, _previousOffset.y);
        }
        
        decalProjector.uvBias = new Vector2(_previousOffset.x + speed * Time.deltaTime, _previousOffset.y);
    }
}