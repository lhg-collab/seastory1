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
    public Sprite octopusIcon;

    [Header("재화 설정")]
    public int currentGold = 0;
    public Text goldTextUI;

    [Header("디버그 지급(테스트용)")]
    public bool enableDebugHotkeys = true;   // 체크하면 단축키 동작
    public int debugAddCount = 1;            // 한 번 누를 때 추가 개수(기본 1)

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
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

        UpdateGoldUI();

        Debug.Log("[InventoryManager] 초기화 완료. 채집 시 아이템이 자동으로 추가됩니다.");
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
            InventorySlot empty = inventorySlots.Find(s => s.GetItem() == null);
            if (empty == null)
            {
                Debug.LogWarning("[InventoryManager] 인벤토리가 가득 찼습니다!");
                return false;
            }

            int toAdd = Mathf.Min(remaining, newItem.maxStack);
            empty.AddItem(newItem, toAdd);
            remaining -= toAdd;
            Debug.Log($"[InventoryManager] 새 슬롯에 {newItem.itemName} {toAdd}개 추가");
        }

        Debug.Log($"[InventoryManager] {newItem.itemName} {count}개 추가 완료!");
        return true;
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

            case ItemType.Octopus:
                return new Item("문어", octopusIcon, ItemType.Octopus, 150); 

            default:
                Debug.LogError($"알 수 없는 아이템 타입: {type}");
                return null;
        }
    }

    // 판매용 아이템 제거
    public bool RemoveItem(InventorySlot slot, int count)
    {
        if (slot.GetItem() == null || slot.GetCount() < count)
        {
            Debug.LogError("아이템 제거 실패: 수량 부족");
            return false;
        }

        int totalPrice = slot.GetItem().sellPrice * count;
        slot.RemoveItem(count);
        AddGold(totalPrice);

        Debug.Log($"{slot.GetItem()?.itemName ?? "아이템"} {count}개 판매 완료 (+{totalPrice} Gold)");
        return true;
    }
    void Update()
    {
        if (!enableDebugHotkeys) return;

        // 1: 전복, 2: 소라, 3: 해삼, 4: 문어
        if (Input.GetKeyDown(KeyCode.Alpha1))
            AddItem(ItemType.Abalone, debugAddCount);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            AddItem(ItemType.Snail, debugAddCount);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            AddItem(ItemType.SeaCucumber, debugAddCount);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            AddItem(ItemType.Octopus, debugAddCount);
    }
}