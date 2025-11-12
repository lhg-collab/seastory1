using UnityEngine;
using System.Linq;

public class J_ShopOpener : MonoBehaviour
{
    [Header("References")]
    public Camera cam;                        // Main Camera 또는 실제 플레이 카메라를 꼭 넣기
    public Transform player;                  // Player Transform(인스펙터에 드래그 추천)
    public LayerMask npcMask;                 // NPC 레이어만 포함
    public J_ShopUIManager shopUI;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float rayLength = 50f;

    J_ShopNPC currentNpc;
    J_NPCHighlighter currentHL;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void OnDisable() => ClearHighlight();

    void Update()
    {
        // 필수 참조 체크 (한 번만 경고)
        if (!cam || !shopUI)
        {
            if (!cam) Debug.LogWarning("[J_ShopOpener] cam이 비었습니다. 인스펙터에 카메라를 넣으세요.", this);
            if (!shopUI) Debug.LogWarning("[J_ShopOpener] shopUI가 비었습니다. J_ShopUIManager를 넣으세요.", this);
            return;
        }

        if (shopUI.panel && shopUI.panel.activeSelf)
        {
            ClearHighlight();
            return;
        }

        // 화면 중앙 레이
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        J_ShopNPC hitNpc = null;

        // NPC 레이어만 검사 (가판대 등에 가려도 NPC만 집힘)
        var hits = Physics.RaycastAll(ray, rayLength, npcMask, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            foreach (var h in hits.OrderBy(h => h.distance))
            {
                var cand = h.collider.GetComponentInParent<J_ShopNPC>();
                if (cand == null) continue;

                // player가 없으면 거리 체크는 생략(가능하면 인스펙터에 드래그로 지정!)
                if (player)
                {
                    float dist = Vector3.Distance(player.position, h.collider.transform.position);
                    if (dist > cand.interactDistance) continue;
                }

                hitNpc = cand;
                break;
            }
        }

        // 하이라이트 토글
        if (hitNpc != currentNpc)
        {
            if (currentHL) currentHL.SetHighlighted(false);
            currentNpc = hitNpc;
            currentHL = currentNpc ? currentNpc.GetComponent<J_NPCHighlighter>() : null;
            if (currentHL) currentHL.SetHighlighted(true);
        }

        if (currentNpc && Input.GetKeyDown(interactKey))
        {
            shopUI.Open(currentNpc);
            ClearHighlight();
        }

#if UNITY_EDITOR
        Debug.DrawRay(ray.origin, ray.direction * 3f, hitNpc ? Color.green : Color.cyan);
#endif
    }

    void ClearHighlight()
    {
        if (currentHL) currentHL.SetHighlighted(false);
        currentNpc = null;
        currentHL = null;
    }
}

// 한 번만 경고 찍는 작은 헬퍼
static class DebugExt
{
    static bool warned;
    public static void DebugLogWarningOnce(this object _, string msg, Object ctx = null)
    {
        if (warned) return; warned = true; Debug.LogWarning(msg, ctx);
    }
}
