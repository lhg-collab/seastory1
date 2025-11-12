using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class J_ShopSellCardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon;
    public Text nameText;
    public Text countPriceText;
    public Image bg;   // 선택(호버용)

    InventorySlot slot;
    J_ShopUIManager manager;

    public void Setup(InventorySlot s, J_ShopUIManager m)
    {
        slot = s; manager = m;
        var item = slot.GetItem();
        if (icon) icon.sprite = item.itemIcon;
        if (nameText) nameText.text = item.itemName;
        if (countPriceText) countPriceText.text = $"x{slot.GetCount()} · {item.sellPrice:N0} G/개";
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Right) return;
        bool sellAll = manager.rightClickMode == J_ShopUIManager.RightClickSellMode.All
                       || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        manager.Sell(slot, sellAll ? slot.GetCount() : 1);
    }

    public void OnPointerEnter(PointerEventData e) { if (bg) bg.color = new Color(1, 1, 1, 0.12f); }
    public void OnPointerExit(PointerEventData e) { if (bg) bg.color = new Color(1, 1, 1, 0.00f); }
}
