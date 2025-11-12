using UnityEngine;
using UnityEngine.UI;
using System;

public class J_ShopSellRowUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Image icon;
    public Text nameText;
    public Text countText;
    public Text unitPriceText;
    public Text totalPriceText;
    public Button sell1Btn;
    public Button sellAllBtn;

    Action onSell1;
    Action onSellAll;

    public void Bind(Sprite spr, string itemName, int count, int unitPrice,
                     Action sellOne, Action sellAll)
    {
        onSell1 = sellOne;
        onSellAll = sellAll;

        if (icon) icon.sprite = spr;
        if (nameText) nameText.text = itemName;
        if (countText) countText.text = $"x{count}";
        if (unitPriceText) unitPriceText.text = $"{unitPrice:N0} G";
        if (totalPriceText) totalPriceText.text = $"{unitPrice * count:N0} G";

        if (sell1Btn)
        {
            sell1Btn.onClick.RemoveAllListeners();
            sell1Btn.onClick.AddListener(() => onSell1?.Invoke());
        }
        if (sellAllBtn)
        {
            sellAllBtn.onClick.RemoveAllListeners();
            sellAllBtn.onClick.AddListener(() => onSellAll?.Invoke());
        }
    }
}
