using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class Item
{
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType;
    public int maxStack = 10;

    // 판매 가격 (NPC에게 팔 때)
    public int sellPrice;

    // 생성자
    public Item(string name, Sprite icon, ItemType type, int price)
    {
        itemName = name;
        itemIcon = icon;
        itemType = type;
        sellPrice = price;
    }
}

public enum ItemType
{
    Abalone,      // 전복
    Snail,        // 소라
    SeaCucumber,  // 해삼
    Fish       // 물고기들
}