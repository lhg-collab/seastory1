using UnityEngine;
using System.Linq;

public class J_ShopOpener : MonoBehaviour
{
    [Header("References")]
    public Camera cam;                         // 플레이 카메라
    public Transform player;                   // Player Transform
    public LayerMask npcMask;                  // NPC 레이어만 포함
    public SellingUIManager sellingUI;         //  여기로 변경

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;    // E키로 열기
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
        if (!sellingUI) sellingUI = FindObjectOfType<SellingUIManager>(true); // 비활성 포함 검색
    }

    void OnDisable() => ClearHighlight();

    void Update()
    {
        if (!cam || !sellingUI) return;

        // 상점 열려 있으면 하이라이트/입력 무시
        if (sellingUI.IsUIOpen())
        {
            ClearHighlight();
            return;
        }

        // 화면 중앙 레이캐스트
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        J_ShopNPC hitNpc = null;

        var hits = Physics.RaycastAll(ray, rayLength, npcMask, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            foreach (var h in hits.OrderBy(h => h.distance))
            {
                var cand = h.collider.GetComponentInParent<J_ShopNPC>();
                if (cand == null) continue;

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

        // E로 열기
        if (currentNpc && Input.GetKeyDown(interactKey))
        {
            sellingUI.OpenUI();
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
