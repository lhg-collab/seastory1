using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    [Header("슬롯 관리")]
    public List<InventorySlot> inventorySlots;

    [Header("아이템 프리셋")]
    public Sprite abaloneIcon;
    public Sprite snailIcon;
    public Sprite seaCucumberIcon;
    public Sprite fishIcon;

    [Header("재화 설정")]
    public int currentGold = 0;
    public Text goldTextUI;
    public Text goldTextUI2;

    [Header("디버그 지급(테스트용)")]
    public bool enableDebugHotkeys = true;   // 체크하면 단축키 동작
    public int debugAddCount = 1;            // 한 번 누를 때 추가 개수(기본 1)

    // === ★ 씬 간 인벤토리/골드 유지용 static 데이터 ===
    static bool s_hasSavedData = false;

    [System.Serializable]
    struct SavedItemData
    {
        public ItemType itemType;
        public int count;
    }

    static List<SavedItemData> s_savedItems = new List<SavedItemData>();
    static int s_savedGold = 0;

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // 인벤토리 슬롯 자동 연결
        if (inventorySlots == null || inventorySlots.Count == 0)
        {
            inventorySlots = new List<InventorySlot>();
            GameObject inventoryPanel = GameObject.Find("InventoryPanel");

            if (inventoryPanel != null)
            {
                InventorySlot[] slots = inventoryPanel.GetComponentsInChildren<InventorySlot>(true);
                foreach (InventorySlot slot in slots)
                {
                    inventorySlots.Add(slot);
                }
                Debug.Log($"인벤토리 슬롯 {inventorySlots.Count}개 연결 완료!");
            }
            else
            {
                Debug.LogError("InventoryPanel을 찾을 수 없습니다!");
            }
        }

        // ★ 이전 씬에서 저장된 인벤토리가 있으면 복원
        if (s_hasSavedData)
        {
            RestoreFromSavedData();
        }
        else
        {
            // 처음 시작일 때는 그냥 현재 골드만 UI에 표시
            UpdateGoldUI();
        }

        Debug.Log("[InventoryManager] 초기화 완료. 채집 시 아이템이 자동으로 추가됩니다.");
    }

    // ★ 이 인스턴스가 파괴될 때(씬 전환 등) 현재 상태를 static에 저장
    void OnDestroy()
    {
        if (instance == this)
        {
            SaveToStatic();
            instance = null;
        }
    }

    // ★ 현재 인벤토리/골드를 static 변수에 저장
    void SaveToStatic()
    {
        if (inventorySlots == null) return;

        s_savedGold = currentGold;
        s_savedItems.Clear();

        foreach (var slot in inventorySlots)
        {
            if (slot == null) continue;
            Item item = slot.GetItem();
            int count = slot.GetCount();
            if (item != null && count > 0)
            {
                s_savedItems.Add(new SavedItemData
                {
                    itemType = item.itemType,
                    count = count
                });
            }
        }

        s_hasSavedData = true;
        Debug.Log($"[InventoryManager] 인벤토리 저장 완료. 골드:{s_savedGold}, 아이템 종류:{s_savedItems.Count}");
    }

    // ★ static 에 저장된 인벤토리를 현재 씬의 슬롯에 복원
    void RestoreFromSavedData()
    {
        currentGold = s_savedGold;
        UpdateGoldUI();

        if (s_savedItems == null || s_savedItems.Count == 0)
            return;

        Debug.Log($"[InventoryManager] 저장된 인벤토리 복원: {s_savedItems.Count}종");

        foreach (var saved in s_savedItems)
        {
            // 슬롯 배치는 새로 해도 되므로, 기존 AddItem 로직 재사용
            AddItem(saved.itemType, saved.count);
        }
    }

    // 골드 추가
    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateGoldUI();
        Debug.Log($"골드 획득! 현재 골드: {currentGold}");
    }

    // UI 갱신
    void UpdateGoldUI()
    {
        if (goldTextUI != null)
            goldTextUI.text = $"{currentGold:N0} Gold";

        if (goldTextUI2 != null)
            goldTextUI2.text = $"{currentGold:N0} Gold";
    }

    // 아이템 추가 (채집 시 호출됨)
    public bool AddItem(ItemType itemType, int count = 1)
    {
        Item newItem = CreateItem(itemType);
        if (newItem == null)
        {
            Debug.LogError($"[InventoryManager] 아이템 생성 실패: {itemType}");
            return false;
        }

        int remaining = count;

        // 1) 기존 슬롯에 같은 아이템이 있으면 합치기
        foreach (var slot in inventorySlots)
        {
            if (slot == null) continue;
            if (remaining <= 0) break;

            var slotItem = slot.GetItem();
            if (slotItem != null && slotItem.itemType == itemType)
            {
                int canAdd = newItem.maxStack - slot.GetCount();
                if (canAdd > 0)
                {
                    int toAdd = Mathf.Min(remaining, canAdd);
                    slot.AddItem(newItem, toAdd);
                    remaining -= toAdd;
                    Debug.Log($"[InventoryManager] 기존 슬롯에 {newItem.itemName} {toAdd}개 추가");
                }
            }
        }

        // 2) 남은 아이템은 빈 슬롯에 추가
        while (remaining > 0)
        {
            InventorySlot empty = inventorySlots.Find(s => s != null && s.GetItem() == null);
            if (empty == null)
            {
                Debug.LogWarning("[InventoryManager] 인벤토리가 가득 찼습니다!");
                break;
            }

            int toAdd = Mathf.Min(remaining, newItem.maxStack);
            empty.AddItem(newItem, toAdd);
            remaining -= toAdd;
            Debug.Log($"[InventoryManager] 새 슬롯에 {newItem.itemName} {toAdd}개 추가");
        }

        int added = count - remaining;
        if (added > 0)
        {
            Debug.Log($"[InventoryManager] {newItem.itemName} {added}개 추가 완료!");

            // ★ static 데이터도 바로 갱신(씬이 안 바뀌어도 항상 최신 상태 유지)
            SyncStaticFromCurrent();
            return true;
        }

        return false;
    }

    // ★ 현재 슬롯 상태를 static 저장 데이터와 동기화
    void SyncStaticFromCurrent()
    {
        if (inventorySlots == null) return;

        s_savedItems.Clear();
        foreach (var slot in inventorySlots)
        {
            if (slot == null) continue;
            Item item = slot.GetItem();
            int count = slot.GetCount();
            if (item != null && count > 0)
            {
                s_savedItems.Add(new SavedItemData
                {
                    itemType = item.itemType,
                    count = count
                });
            }
        }

        s_savedGold = currentGold;
        s_hasSavedData = true;
    }

    // 아이템 생성 (타입 기준)
    Item CreateItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Abalone:
                return new Item("전복", abaloneIcon, ItemType.Abalone, 100);

            case ItemType.Snail:
                return new Item("소라", snailIcon, ItemType.Snail, 50);

            case ItemType.SeaCucumber:
                return new Item("해삼", seaCucumberIcon, ItemType.SeaCucumber, 70);

            case ItemType.Fish:
                return new Item("생선", fishIcon, ItemType.Fish, 150);

            default:
                Debug.LogError($"알 수 없는 아이템 타입: {type}");
                return null;
        }
    }

    // 판매용 아이템 제거 (지금은 주로 SellingUIManager 쪽에서 직접 RemoveItem/Gold 처리 중)
    public bool RemoveItem(InventorySlot slot, int count)
    {
        if (slot.GetItem() == null || slot.GetCount() < count)
        {
            Debug.LogError("아이템 제거 실패: 수량 부족");
            return false;
        }

        Item item = slot.GetItem();
        if (item == null) return false;

        int totalPrice = item.sellPrice * count;
        slot.RemoveItem(count);
        AddGold(totalPrice);

        Debug.Log($"{item.itemName} {count}개 판매 완료 (+{totalPrice} Gold)");

        // ★ 제거 후에도 static 동기화
        SyncStaticFromCurrent();
        return true;
    }

    void Update()
    {
        if (!enableDebugHotkeys) return;

        // 1: 전복, 2: 소라, 3: 해삼, 4: 생선
        if (Input.GetKeyDown(KeyCode.Alpha1))
            AddItem(ItemType.Abalone, debugAddCount);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            AddItem(ItemType.Snail, debugAddCount);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            AddItem(ItemType.SeaCucumber, debugAddCount);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            AddItem(ItemType.Fish, debugAddCount);
    }
}
