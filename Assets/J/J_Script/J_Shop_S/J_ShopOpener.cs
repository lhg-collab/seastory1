using UnityEngine;
using System.Linq;

public class J_ShopOpener : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform player;
    public LayerMask npcMask;            // NPC 레이어만
    public SellingUIManager sellingUI;   // ← 원래 상점 매니저

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float rayLength = 50f;

    J_ShopNPC currentNpc;
    J_NPCHighlighter currentHL;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!player) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.transform; }
        if (!sellingUI) sellingUI = FindObjectOfType<SellingUIManager>(true);
    }

    void OnDisable() => ClearHL();

    void Update()
    {
        if (!cam || !sellingUI) return;
        if (sellingUI.IsUIOpen()) { ClearHL(); return; }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        J_ShopNPC hitNpc = null;

        var hits = Physics.RaycastAll(ray, rayLength, npcMask, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            foreach (var h in hits.OrderBy(h => h.distance))
            {
                var cand = h.collider.GetComponentInParent<J_ShopNPC>();
                if (!cand) continue;
                if (player && Vector3.Distance(player.position, h.collider.transform.position) > cand.interactDistance)
                    continue;
                hitNpc = cand; break;
            }
        }

        if (hitNpc != currentNpc)
        {
            if (currentHL) currentHL.SetHighlighted(false);
            currentNpc = hitNpc;
            currentHL = currentNpc ? currentNpc.GetComponent<J_NPCHighlighter>() : null;
            if (currentHL) currentHL.SetHighlighted(true);
        }

        if (currentNpc && Input.GetKeyDown(interactKey))
        {
            sellingUI.OpenUI();
            ClearHL();
        }
    }

    void ClearHL()
    {
        if (currentHL) currentHL.SetHighlighted(false);
        currentNpc = null; currentHL = null;
    }
}
