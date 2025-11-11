using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Gatherable : MonoBehaviour
{
    [Header("FX (º±≈√)")]
    public GameObject collectedVfx;
    public AudioClip collectedSfx;
    public bool hideOnCollected = true;

    [HideInInspector] public GatherableSpawner ownerSpawner;
    public UnityEvent onCollected;

    bool _available = true;

    public bool TryCollect(Transform by)
    {
        if (!_available) return false;
        _available = false;

        onCollected?.Invoke();

        if (hideOnCollected)
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        }

        if (collectedVfx) Instantiate(collectedVfx, transform.position, Quaternion.identity);
        if (collectedSfx) AudioSource.PlayClipAtPoint(collectedSfx, transform.position);

        ownerSpawner?.NotifyCollected(this);
        return true;
    }
}
