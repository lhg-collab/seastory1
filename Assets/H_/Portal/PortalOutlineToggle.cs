// 포탈에 붙여서 가까이 있을 때만 머티리얼 토글
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalOutlineToggle : MonoBehaviour
{
    public Renderer[] targetRenderers;
    public Material glowMaterial;   // SG_PortalGlow로 만든 머티리얼
    public bool addInsteadOfReplace = true; // 기존 머티리얼 유지 + 추가

    bool playerInside;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        SetGlow(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        SetGlow(false);
    }

    void SetGlow(bool on)
    {
        if (!glowMaterial) return;
        foreach (var r in targetRenderers)
        {
            if (!r) continue;
            var mats = r.sharedMaterials;
            if (addInsteadOfReplace)
            {
                // 이미 추가돼 있으면 스킵
                if (on)
                {
                    bool has = false;
                    foreach (var m in mats) if (m == glowMaterial) { has = true; break; }
                    if (!has)
                    {
                        var list = new System.Collections.Generic.List<Material>(mats);
                        list.Add(glowMaterial);
                        r.sharedMaterials = list.ToArray();
                    }
                }
                else
                {
                    var list = new System.Collections.Generic.List<Material>();
                    foreach (var m in mats) if (m != glowMaterial) list.Add(m);
                    r.sharedMaterials = list.ToArray();
                }
            }
            else
            {
                // 교체 모드
                if (on) r.sharedMaterial = glowMaterial;
                // off일 때는 원복 로직 필요 -> addInsteadOfReplace 권장
            }
        }
    }
}
