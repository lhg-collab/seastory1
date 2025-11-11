using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image itemIcon;
    public Text itemCountText;

    Item currentItem;
    int currentCount = 0;

    void Start()
    {
        if (currentItem != null) UpdateSlotUI();
        else HideUI();
    }

    // 아이템 추가
    public bool AddItem(Item item, int count = 1)
    {
        if (currentItem == null)
        {
            currentItem = item;
            currentCount = count;
            UpdateSlotUI();
            return true;
        }
        else if (currentItem.itemType == item.itemType)
        {
            if (currentCount + count <= currentItem.maxStack)
            {
                currentCount += count;
                UpdateSlotUI();
                return true;
            }
            else
            {
                Debug.Log("더 이상 쌓을 수 없습니다!");
                return false;
            }
        }
        else
        {
            Debug.Log("다른 아이템이 있습니다!");
            return false;
        }
    }

    // 아이템 제거 (판매 시)
    public void RemoveItem(int count)
    {
        currentCount -= count;
        if (currentCount <= 0) ClearSlot();
        else UpdateSlotUI();
    }

    // 슬롯 UI 업데이트
    void UpdateSlotUI()
    {
        if (currentItem != null)
        {
            itemIcon.sprite = currentItem.itemIcon;
            itemIcon.enabled = true;
            itemCountText.text = currentCount.ToString();
            itemCountText.enabled = true;
        }
        else
        {
            HideUI();
        }
    }

    void HideUI()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
        if (itemCountText != null)
        {
            itemCountText.text = "";
            itemCountText.enabled = false;
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        currentCount = 0;
        HideUI();
    }

    public Item GetItem() => currentItem;
    public int GetCount() => currentCount;
}