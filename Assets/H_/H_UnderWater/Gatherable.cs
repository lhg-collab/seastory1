using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Gatherable : MonoBehaviour
{
    [Header("FX (선택)")]
    public GameObject collectedVfx;
    public AudioClip collectedSfx;
    public bool hideOnCollected = true;

    [Header("인벤토리 보상 설정")]
    [Tooltip("채집 시 인벤토리에 아이템을 지급할지 여부")]
    public bool giveItemOnCollect = true;
    public ItemType itemType;          // 전복/소라/해삼/물고기 등
    [Min(1)] public int amount = 1;    // 한 번에 줄 개수

    [HideInInspector] public GatherableSpawner ownerSpawner;
    public UnityEvent onCollected;

    bool _available = true;

    public bool TryCollect(Transform by)
    {
        if (!_available) return false;
        _available = false;

        // 유니티 이벤트 먼저
        onCollected?.Invoke();

        // ★ 1) 인벤토리에 아이템 지급
        if (giveItemOnCollect && InventoryManager.instance != null)
        {
            InventoryManager.instance.AddItem(itemType, amount);
            Debug.Log($"[Gatherable] {itemType} {amount}개 인벤토리에 지급");
        }

        // ★ 2) 튜토리얼에 '채집했다' 알리기 (필요한 씬에서만 조건 검사됨)
        var ui = FindObjectOfType<HaenyeoUIManager>();
        if (ui != null)
        {
            ui.OnItemCollected();
        }

        // 원래 있던 처리들
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
