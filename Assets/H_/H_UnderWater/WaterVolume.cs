using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterVolume : MonoBehaviour
{
    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    public float SurfaceY => _col.bounds.max.y;
    public float BottomY => _col.bounds.min.y;
    public float Height => _col.bounds.size.y;

    private void OnTriggerEnter(Collider other)
    {
        var swim = other.GetComponentInParent<SwimController>();
        if (swim != null) swim.EnterWater(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var swim = other.GetComponentInParent<SwimController>();
        if (swim != null) swim.ExitWater(this);
    }
}
