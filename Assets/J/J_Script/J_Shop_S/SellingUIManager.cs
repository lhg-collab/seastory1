using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Starter Assets & Cinemachine 연동(플레이어 조작 잠그기/복구)
using StarterAssets;
using Cinemachine;

/// 상점 UI 관리자 - 고정 진열 방식
/// 항상 같은 아이템 슬롯을 표시하고, 재고 유무만 표시
public class SellingUIManager : MonoBehaviour
{
    [Header("상점 UI 오브젝트")]
    public GameObject sellingPanel;     // 상점 전체 패널(비활성로 시작 권장)
    public Transform slotParent;        // 슬롯이 생성될 부모 Transform
    public GameObject slotPrefab;       // 슬롯 프리팹 (SellingSlot)
    public Button closeButton;          // 닫기 버튼

    [Header("판매 가능 아이템 목록")]
    public List<ItemType> shopItemTypes = new List<ItemType>(); // 상점에서 판매받을 아이템 종류

    // ── Starter Assets(3인칭) 컨트롤 잠그기용 ──
    [Header("Player Control Lock")]
    [SerializeField] StarterAssetsInputs inputs;
    [SerializeField] ThirdPersonController tpc;
    [SerializeField] CinemachineInputProvider cinLook;

    // 원상복구용 캐시
    bool wasTPCEnabled, wasCinEnabled, wasCursorLocked, wasCursorLook;

    private bool isOpen = false;        // 상점이 열려있는지 여부

    void Awake()
    {
        // 닫기 버튼 연결
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);

        // 시작 시 패널 끄기
        if (sellingPanel != null)
        {
            sellingPanel.SetActive(false);
            Debug.Log("상점 패널 비활성화 완료");
        }

        // 기본 아이템 목록(없으면 채움)
        if (shopItemTypes.Count == 0)
        {
            shopItemTypes.Add(ItemType.Abalone);      // 전복
            shopItemTypes.Add(ItemType.Snail);        // 소라
            shopItemTypes.Add(ItemType.SeaCucumber);  // 해삼
            shopItemTypes.Add(ItemType.Fish);      // 생선
        }

        // 플레이어 컴포넌트 자동 찾기
        if (!inputs) inputs = FindObjectOfType<StarterAssetsInputs>(true);
        if (!tpc) tpc = FindObjectOfType<ThirdPersonController>(true);
        if (!cinLook) cinLook = FindObjectOfType<CinemachineInputProvider>(true);
    }

    void EnsureEventSystem()
    {
        // 씬(비활성 포함)에 있는 EventSystem 전부 조사
        var all = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>(true);
        if (all != null && all.Length > 0)
        {
            // 하나라도 활성이라면 새로 만들 필요 없음
            foreach (var es in all) if (es.isActiveAndEnabled) return;

            // 전부 비활성이면 첫 번째를 켜서 사용
            all[0].gameObject.SetActive(true);
            return;
        }

        // 여기까지 오면 정말로 하나도 없을 때만 생성
        var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
    go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
    }

    // 🔹 Canvas/Raycaster/CanvasGroup 보정
    void EnsureCanvasClickable()
    {
        if (!sellingPanel) return;

        // 1) Canvas 확보
        var canvas = sellingPanel.GetComponentInParent<Canvas>(true);
        if (!canvas)
        {
            canvas = sellingPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // 2) GraphicRaycaster 필수
        var raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (!raycaster) canvas.gameObject.AddComponent<GraphicRaycaster>();

        // 3) Camera 보정 (Camera/World 모드일 때 필수)
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }

        // 4) CanvasGroup 보정
        var cg = sellingPanel.GetComponent<CanvasGroup>();
        if (!cg) cg = sellingPanel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // 5) 전면 투명 오버레이 무력화(페이드류)
        var overlays = canvas.GetComponentsInChildren<CanvasGroup>(true);
        foreach (var ocg in overlays)
        {
            if (ocg == cg) continue;
            var n = ocg.name.ToLower();
            if ((n.Contains("fader") || n.Contains("fade") || n.Contains("blocker")) && ocg.alpha < 0.01f)
            {
                ocg.blocksRaycasts = false;
            }
        }
    }

    /// 상점 열기
    public void OpenUI()
    {
        if (isOpen)
        {
            Debug.Log("상점이 이미 열려있습니다!");
            return;
        }

        isOpen = true;

        EnsureEventSystem();
        if (sellingPanel != null)
        {
            sellingPanel.SetActive(true);
            EnsureCanvasClickable();
            Debug.Log("상점 UI 열림");
        }

        // 커서 UI 모드
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 슬롯 생성
        GenerateSellingSlots();

        // ── 플레이어 조작 잠그기(Starter Assets) ──
        if (inputs)
        {
            wasCursorLocked = inputs.cursorLocked;
            wasCursorLook = inputs.cursorInputForLook;
            inputs.cursorLocked = false;
            inputs.cursorInputForLook = false;
        }
        if (tpc)
        {
            wasTPCEnabled = tpc.enabled;
            tpc.enabled = false;                 // 이동/점프 멈춤
            tpc.LockCameraPosition = true;       // 안전하게 카메라 고정
        }
        if (cinLook)
        {
            wasCinEnabled = cinLook.enabled;
            cinLook.enabled = false;             // 마우스 룩 차단
        }
    }

    /// 상점 닫기 (닫기 버튼 클릭 시 호출)
    public void CloseUI()
    {
        isOpen = false;

        if (sellingPanel != null)
        {
            sellingPanel.SetActive(false);
            Debug.Log("상점 UI 닫힘");
        }

        // 상점 닫기 소리
        if (AudioManager.instance != null)
            AudioManager.instance.PlayShopSound();

        // 커서 잠금 복귀
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ── 플레이어 조작 복구 ──
        if (inputs)
        {
            inputs.cursorLocked = wasCursorLocked;
            inputs.cursorInputForLook = wasCursorLook;
        }
        if (tpc)
        {
            tpc.enabled = wasTPCEnabled;
            tpc.LockCameraPosition = false;
        }
        if (cinLook)
        {
            cinLook.enabled = wasCinEnabled;
        }
    }

    /// 판매 슬롯 생성 - 고정 진열 방식
    void GenerateSellingSlots()
    {
        if (slotParent == null || slotPrefab == null)
        {
            Debug.LogError("SlotParent 또는 SlotPrefab이 null입니다!");
            return;
        }

        InventoryManager inv = InventoryManager.instance;
        if (inv == null)
        {
            Debug.LogError("InventoryManager를 찾을 수 없습니다!");
            return;
        }

        // 기존 슬롯 제거
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        // 각 아이템 타입별로 슬롯 생성
        foreach (ItemType itemType in shopItemTypes)
        {
            // 1. 인벤토리에서 해당 아이템 찾기
            InventorySlot invSlot = FindInventorySlot(itemType);

            // 2. 아이템 데이터 생성 (이름, 아이콘, 가격 등)
            Item itemData = CreateItemData(itemType);
            if (itemData == null) continue;

            // 3. 슬롯 오브젝트 생성
            GameObject slotObj = Instantiate(slotPrefab, slotParent);
            SellingSlot slot = slotObj.GetComponent<SellingSlot>();

            if (slot != null)
            {
                // 4. 재고 개수 확인
                int count = invSlot != null ? invSlot.GetCount() : 0;
                bool hasStock = count > 0;

                // 5. 슬롯 세팅
                slot.SetupSlot(itemData, count, invSlot, this, hasStock);
            }
        }

        Debug.Log($"{shopItemTypes.Count}개의 판매 슬롯 생성 완료!");
    }

    /// 인벤토리에서 특정 아이템 타입의 슬롯 찾기
    InventorySlot FindInventorySlot(ItemType itemType)
    {
        InventoryManager inv = InventoryManager.instance;
        if (inv == null) return null;

        foreach (InventorySlot slot in inv.inventorySlots)
        {
            Item item = slot.GetItem();
            if (item != null && item.itemType == itemType && slot.GetCount() > 0)
                return slot;
        }
        return null;
    }

    /// 아이템 타입으로 아이템 데이터 생성 (InventoryManager의 아이콘/가격 사용)
    Item CreateItemData(ItemType itemType)
    {
        InventoryManager inv = InventoryManager.instance;
        if (inv == null) return null;

        switch (itemType)
        {
            case ItemType.Abalone: return new Item("전복", inv.abaloneIcon, ItemType.Abalone, 100);
            case ItemType.Snail: return new Item("소라", inv.snailIcon, ItemType.Snail, 50);
            case ItemType.SeaCucumber: return new Item("해삼", inv.seaCucumberIcon, ItemType.SeaCucumber, 70);
            case ItemType.Fish: return new Item("생선", inv.fishIcon, ItemType.Fish, 150);
            default: return null;
        }
    }

    /// 판매 시도 - 슬롯에서 좌클릭 시 호출
    public void TrySell(ItemType itemType, InventorySlot invSlot)
    {
        if (invSlot == null || invSlot.GetCount() <= 0)
        {
            Debug.Log("판매할 아이템이 부족합니다!");
            if (AudioManager.instance != null) AudioManager.instance.PlayOutOfStock();
            return;
        }

        InventoryManager inv = InventoryManager.instance;
        if (inv == null) return;

        Item item = invSlot.GetItem();
        if (item == null) return;

        int count = invSlot.GetCount();
        int totalPrice = item.sellPrice * count;

        invSlot.RemoveItem(count);
        inv.AddGold(totalPrice);

        if (AudioManager.instance != null) AudioManager.instance.PlayCoinSound();

        Debug.Log($"{item.itemName} {count}개 판매 완료! +{totalPrice} Gold");

        RefreshSlots(); // 상태 새로고침
    }

    /// 모든 슬롯 새로고침 - 재고 상태 업데이트
    public void RefreshSlots()
    {
        if (slotParent == null) return;

        foreach (Transform child in slotParent)
        {
            var slot = child.GetComponent<SellingSlot>();
            if (slot != null) slot.RefreshState();
        }
    }

    /// 상점이 열려있는지 확인 (NpcInteraction에서 사용)
    public bool IsUIOpen() => isOpen;
}
