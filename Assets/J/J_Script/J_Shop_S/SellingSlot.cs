using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// 상점 판매 슬롯 - 고정 진열 방식
/// 인벤토리 재고 유무에 따라 색상 변경 및 판매 가능 여부 결정
public class SellingSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image itemIcon;          // 아이템 아이콘 이미지
    public Text itemNameText;       // 아이템 이름 텍스트
    public Text priceText;          // 가격 텍스트
    public Image background;        // 슬롯 배경 이미지 (색상 변경용)

    [Header("Color Settings")]
    public Color availableColor = new Color(1f, 1f, 1f, 1f);        // 판매 가능 (밝은 색)
    public Color unavailableColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 판매 불가 (어두운 색)

    private Item currentItem;               // 이 슬롯의 아이템 데이터
    private int currentCount;               // 현재 인벤토리 보유 개수
    private ItemType itemType;              // 아이템 타입 (재고 확인용)
    private InventorySlot inventorySlot;    // 연결된 인벤토리 슬롯 (실제 아이템 있는 곳)
    private SellingUIManager uiManager;     // 상점 UI 매니저 참조
    private bool hasStock;                  // 재고 있는지 여부 (true = 판매 가능)

    /// 슬롯 초기 세팅
    /// 상점이 열릴 때 SellingUIManager에서 호출
    public void SetupSlot(Item item, int count, InventorySlot invSlot, SellingUIManager manager, bool inStock)
    {
        currentItem = item;
        currentCount = count;
        itemType = item.itemType;
        inventorySlot = invSlot;
        uiManager = manager;
        hasStock = inStock;

        UpdateUI(); // UI 업데이트
    }
    /// UI 업데이트 - 아이콘, 텍스트, 색상 갱신
    void UpdateUI()
    {
        // ========== 아이콘 설정 ==========
        if (itemIcon != null && currentItem != null)
        {
            itemIcon.sprite = currentItem.itemIcon;
        }

        // ========== 이름 + 개수 설정 ==========
        if (itemNameText != null && currentItem != null)
        {
            if (hasStock)
            {
                // 재고 있으면: "전복 x3"
                itemNameText.text = currentItem.itemName + " x" + currentCount;
            }
            else
            {
                // 재고 없으면: "전복 x0"
                itemNameText.text = currentItem.itemName + " x0";
            }
        }

        // ========== 가격 설정 ==========
        if (priceText != null && currentItem != null)
        {
            if (hasStock)
            {
                // 재고 있으면: 총 판매 가격 (개당 가격 x 개수)
                int totalPrice = currentItem.sellPrice * currentCount;
                priceText.text = totalPrice.ToString();
            }
            else
            {
                // 재고 없으면: 개당 가격만 표시
                priceText.text = currentItem.sellPrice.ToString();
            }
        }

        // ========== 색상 변경 (재고 유무) ==========
        // 재고 있으면 밝은 색, 없으면 어두운 색
        Color targetColor = hasStock ? availableColor : unavailableColor;

        // 모든 UI 요소에 색상 적용
        if (itemIcon != null)
            itemIcon.color = targetColor;

        if (itemNameText != null)
            itemNameText.color = targetColor;

        if (priceText != null)
            priceText.color = targetColor;

        if (background != null)
            background.color = targetColor;

        // 디버그 로그
        UnityEngine.Debug.Log("슬롯 세팅: " + (currentItem != null ? currentItem.itemName : "null") +
                              ", 재고: " + hasStock + ", 개수: " + currentCount);
    }
    /// 마우스 클릭 이벤트 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        // 우클릭 감지
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            UnityEngine.Debug.Log((currentItem != null ? currentItem.itemName : "null") + " 좌클릭!");
            SellItem(); // 판매 시도
        }
    }
    /// 아이템 판매 시도
    /// 재고가 있으면 판매, 없으면 메시지만 출력
    void SellItem()
    {
        if(currentItem == null || uiManager == null)
        {
            // ========== 유효성 검사 ==========
            UnityEngine.Debug.LogError("currentItem 또는 uiManager가 null입니다!");
            return;
        }
        // 판매 소리 재생
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayCoinSound();
        }
        // ========== 재고 확인 ==========
        if (!hasStock)
        {
            // 재고 없을 때 소리
            if(AudioManager.instance != null)
            {
                AudioManager.instance.PlayOutOfStock();
            }

            Debug.Log("판매할 아이템이 부족합니다.");
            return;
        }

        // ========== 판매 진행 ==========
        UnityEngine.Debug.Log(currentItem.itemName + " " + currentCount + "개 판매 시도!");

        // SellingUIManager에게 판매 요청
        uiManager.TrySell(itemType, inventorySlot);
    }
    /// 슬롯 상태 새로고침
    /// 판매 후 재고 상태 업데이트 (SellingUIManager에서 호출)
    public void RefreshState()
    {
        // ========== 인벤토리 슬롯이 있는 경우 ==========
        if (inventorySlot != null)
        {
            // 현재 개수 다시 확인
            currentCount = inventorySlot.GetCount();
            hasStock = currentCount > 0;
        }
        // ========== 인벤토리 슬롯이 없는 경우 (재고 소진 후) ==========
        else
        {
            // 인벤토리에서 다시 찾아보기
            InventoryManager inv = InventoryManager.instance;
            if (inv != null)
            {
                // 모든 인벤토리 슬롯 순회
                foreach (InventorySlot slot in inv.inventorySlots)
                {
                    Item item = slot.GetItem();

                    // 같은 타입 아이템이고 개수가 있으면
                    if (item != null && item.itemType == itemType && slot.GetCount() > 0)
                    {
                        // 연결 복구
                        inventorySlot = slot;
                        currentCount = slot.GetCount();
                        hasStock = true;
                        UpdateUI();
                        return;
                    }
                }
            }

            // 못 찾으면 재고 없음 처리
            currentCount = 0;
            hasStock = false;
        }

        // UI 업데이트 (색상 변경)
        UpdateUI();
    }
}