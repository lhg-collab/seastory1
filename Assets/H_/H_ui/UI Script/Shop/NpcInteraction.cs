using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;   // UI 위 클릭 무시용

public class NpcInteraction : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform playerTransform;
    public float interactionDistance = 3f; // NPC로부터 3미터 이내에서만 상호작용 가능

    [Header("UI Manager")]
    public SellingUIManager sellingUIManager;

    [Header("Auto Find Shop UI")]
    public bool autoFindShopUI = true;
    public string shopPanelName = "ShopPanel";
    public bool includeInactiveUI = true;     // 비활성/DDOL 포함 탐색
    public float uiFindRetryInterval = 0.5f;  // 재시도 간격(초)

    float _nextFindTime;

    void Start()
    {
        // 플레이어 자동 찾기
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("플레이어를 찾았습니다: " + player.name);
            }
            else
            {
                Debug.LogWarning("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
            }
        }

        // 상점 UI 자동 연결 1차 시도
        if (autoFindShopUI && sellingUIManager == null)
        {
            TryFindSellingUIOnce(out sellingUIManager);
            _nextFindTime = Time.unscaledTime + uiFindRetryInterval;
        }
    }

    void Update()
    {
        // 필요 시 주기적으로 재시도
        if (autoFindShopUI && sellingUIManager == null && Time.unscaledTime >= _nextFindTime)
        {
            if (TryFindSellingUIOnce(out sellingUIManager))
            {
                Debug.Log($"[NpcInteraction] SellingUIManager 자동 연결: {sellingUIManager.name}");
            }
            _nextFindTime = Time.unscaledTime + uiFindRetryInterval;
        }

        // 상점이 열려 있으면 입력 무시
        if (sellingUIManager != null && sellingUIManager.IsUIOpen()) return;

        // 마우스가 UI 위에 있으면 NPC 클릭 무시
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        // 우클릭 감지
        if (Input.GetMouseButtonDown(1)) // 1 = 우클릭
        {
            var cam = Camera.main;
            if (!cam) return;

            // 마우스 위치에서 Ray 발사
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // Raycast로 NPC 클릭 확인
            if (Physics.Raycast(ray, out var hit))
            {
                // ※ 자식 콜라이더까지 허용하려면 GetComponentInParent<NpcInteraction>()로 확인
                var owner = hit.collider.GetComponentInParent<NpcInteraction>();
                if (owner == this)
                {
                    // 거리 체크
                    if (IsPlayerNearby())
                    {
                        // NPC 클릭 시 소리
                        if (AudioManager.instance != null)
                            AudioManager.instance.PlayShopSound();

                        OpenShop();
                    }
                    else
                    {
                        Debug.Log("NPC가 너무 멀어요!");
                    }
                }
            }
        }
    }

    // 플레이어가 상호작용 반경 내에 있는지 확인
    bool IsPlayerNearby()
    {
        if (playerTransform == null)
        {
            Debug.LogError("플레이어 오브젝트가 연결되지 않았습니다.");
            return false;
        }

        float distance = Vector3.Distance(this.transform.position, playerTransform.position);
        Debug.Log($"플레이어와의 거리: {distance}m (상호작용 거리: {interactionDistance}m)");
        return distance <= interactionDistance;
    }

    // 상점 열기
    void OpenShop()
    {
        if (sellingUIManager != null)
        {
            sellingUIManager.OpenUI();
        }
        else
        {
            Debug.LogError("SellingUIManager가 연결되지 않았습니다!");
        }
    }

    // --------- 여기부터 자동 탐색 로직 ---------
    bool TryFindSellingUIOnce(out SellingUIManager mgr)
    {
        mgr = null;

        // (A) 이름으로 우선: ShopPanel
        var target = FindTransformDeepByName(shopPanelName, includeInactiveUI);
        if (target)
        {
            mgr = target.GetComponent<SellingUIManager>()
               ?? target.GetComponentInChildren<SellingUIManager>(true)
               ?? target.GetComponentInParent<SellingUIManager>(true);
            if (mgr) return true;
        }

        // (B) 씬 전체에서 컴포넌트로 탐색(비활성 포함)
#if UNITY_2020_1_OR_NEWER
        var all = Resources.FindObjectsOfTypeAll<SellingUIManager>();
#else
        var all = GameObject.FindObjectsOfType<SellingUIManager>();
#endif
        // ShopPanel 이름을 가진 것부터 선호
        mgr = all.FirstOrDefault(x => x && x.name == shopPanelName) ?? all.FirstOrDefault();
        return mgr != null;
    }

    static Transform FindTransformDeepByName(string targetName, bool includeInactive)
    {
#if UNITY_2020_1_OR_NEWER
        var all = includeInactive ? Resources.FindObjectsOfTypeAll<Transform>()
                                  : Object.FindObjectsOfType<Transform>();
#else
        var all = Object.FindObjectsOfType<Transform>();
#endif
        return all.FirstOrDefault(t => t && t.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase));
    }
}
