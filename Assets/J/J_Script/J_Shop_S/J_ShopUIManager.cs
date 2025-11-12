using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class J_ShopUIManager : MonoBehaviour
{
    public enum RightClickSellMode { One, All }

    [Header("UI")]
    public GameObject panel;              // J_ShopPanel
    public Transform content;             // J_GridContent
    public J_ShopSellCardUI cardPrefab;   // 프리팹
    public Button closeButton;            // J_CloseButton
    public GameObject emptyHint;          // J_Hint (선택)
    public Text shopGoldText;

    [Header("Options")]
    public KeyCode closeKey = KeyCode.Escape;

    [Tooltip("상점 열릴 때 커서 보이기/잠금 해제")]
    public bool unlockCursorWhileOpen = true;

    [Tooltip("상점 열리면 Time.timeScale=0으로 일시정지")]
    public bool pauseTimeWhileOpen = true;

    public RightClickSellMode rightClickMode = RightClickSellMode.One;

    [Header("Lock Control While Open")]
    [Tooltip("열려있을 때 끌 컴포넌트들(예: PlayerInput, ThirdPersonController, CinemachineInputProvider 등)")]
    public Behaviour[] disableComponents;

    [Tooltip("씬에서 Cinemachine 입력 컴포넌트를 자동으로 찾아서 같이 잠급니다")]
    public bool autoLockCinemachineInputs = true;

    // 내부 상태
    readonly List<J_ShopSellCardUI> cards = new();
    readonly List<Behaviour> reEnableCache = new();
    readonly List<Behaviour> autoFoundCmInputs = new();

    CursorLockMode prevCursorLock;
    bool prevCursorVisible;
    float prevTimeScale = 1f;

    void Start()
    {
        if (closeButton) closeButton.onClick.AddListener(Close);
        Close();
    }

    void Update()
    {
        if (panel.activeSelf && Input.GetKeyDown(closeKey)) Close();
    }

    public void Open(J_ShopNPC npc)
    {
        panel.SetActive(true);

        // 커서
        prevCursorLock = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
        if (unlockCursorWhileOpen) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

        // 시간
        if (pauseTimeWhileOpen) { prevTimeScale = Time.timeScale; Time.timeScale = 0f; }

        // 입력/카메라 잠금
        CollectCinemachineInputs();   // ← 자동 탐색
        LockDisableComponents(true);

        Rebuild();
        RefreshShopGold();
    }

    public void Close()
    {
        panel.SetActive(false);

        // 입력/카메라 원복
        LockDisableComponents(false);

        // 시간/커서 원복
        if (pauseTimeWhileOpen) Time.timeScale = prevTimeScale;
        if (unlockCursorWhileOpen) { Cursor.lockState = prevCursorLock; Cursor.visible = prevCursorVisible; }
    }

    void LockDisableComponents(bool on)
    {
        // 수동으로 넣은 것들
        if (disableComponents != null)
        {
            if (on)
            {
                foreach (var b in disableComponents) if (b && b.enabled) { b.enabled = false; reEnableCache.Add(b); }
            }
            else
            {
                foreach (var b in reEnableCache) if (b) b.enabled = true;
                reEnableCache.Clear();
            }
        }

        // 자동으로 찾은 시네머신 입력들
        if (autoFoundCmInputs.Count > 0)
        {
            if (on)
            {
                foreach (var b in autoFoundCmInputs) if (b && b.enabled) { b.enabled = false; reEnableCache.Add(b); }
            }
            else
            {
                foreach (var b in reEnableCache) if (b) b.enabled = true;
                reEnableCache.Clear();
            }
        }
    }

    // Cinemachine 입력 컴포넌트 자동 검색 (의존성 없이 이름으로 식별)
    void CollectCinemachineInputs()
    {
        autoFoundCmInputs.Clear();
        if (!autoLockCinemachineInputs) return;

        // 비활성 포함 전역 검색
        var all = Object.FindObjectsOfType<Behaviour>(true);
        foreach (var b in all)
        {
            if (!b) continue;
            string n = b.GetType().FullName; // 예) "Cinemachine.CinemachineInputProvider"
            if (n == null) continue;

            // 흔히 쓰이는 입력 관련 컴포넌트들
            if (n == "Cinemachine.CinemachineInputProvider" ||
                n == "Cinemachine.CinemachineInputAxisController" ||  // CM3
                n == "Cinemachine.CinemachinePOV")                    // 마우스 축 직접 처리
            {
                autoFoundCmInputs.Add(b);
            }
        }
    }

    void Rebuild()
    {
        // 기존 카드 삭제
        foreach (var c in cards) if (c) Destroy(c.gameObject);
        cards.Clear();

        // 0) 필수 참조 확인
        if (!content) { Debug.LogError("[Shop] content(=J_GridContent) 미할당"); return; }
        if (!cardPrefab) { Debug.LogError("[Shop] cardPrefab(=J_ShopSellCardUI 프리팹) 미할당"); return; }

        var inv = InventoryManager.instance;
        if (inv == null) { Debug.LogError("[Shop] InventoryManager.instance == null"); return; }
        if (inv.inventorySlots == null) { Debug.LogError("[Shop] inventorySlots == null"); return; }

        int listed = 0, haveItems = 0;

        for (int i = 0; i < inv.inventorySlots.Count; i++)
        {
            var slot = inv.inventorySlots[i];
            if (slot == null) { Debug.LogWarning($"[Shop] slot {i} == null"); continue; }

            var item = slot.GetItem();
            int count = slot.GetCount();

            if (item == null || count <= 0) continue;
            haveItems++;

            if (item.sellPrice <= 0)
            {
                Debug.LogWarning($"[Shop] '{item.itemName}' sellPrice=0 → 목록에서 제외");
                continue;
            }

            var card = Instantiate(cardPrefab, content);
            card.Setup(slot, this);
            cards.Add(card);
            listed++;
        }

        if (emptyHint) emptyHint.SetActive(listed == 0);

        Debug.Log($"[Shop] 슬롯:{inv.inventorySlots.Count}, 아이템있는슬롯:{haveItems}, 표시된카드:{listed}");
    }


    void RefreshShopGold()
    {
        if (!shopGoldText) return;
        int gold = 0;
        var inv = InventoryManager.instance;
        if (inv != null)
        {
            // 여기를 프로젝트 변수명으로 교체
            // gold = inv.Gold;
        }
        shopGoldText.text = $"소지금 {gold:N0} G";
    }

    public void Sell(InventorySlot slot, int amount)
    {
        amount = Mathf.Clamp(amount, 1, slot.GetCount());
        var inv = InventoryManager.instance;
        if (inv != null && inv.RemoveItem(slot, amount))
        {
            Rebuild();
            RefreshShopGold();
        }
    }
}
