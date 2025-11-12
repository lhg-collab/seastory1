using System.Collections.Generic;
using UnityEngine;

public class J_NPCHighlighter : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("하이라이트할 렌더러들(비우면 자식에서 자동 탐색)")]
    public Renderer[] targets;
    public bool includeChildrenIfEmpty = true;

    [Header("Outline")]
    [Tooltip("원본(에셋) 머티리얼. 런타임에 안전하게 복제해서 사용합니다.")]
    public Material outlineMaterial; // ex) J_Mat_Outline

    // 캐시: off(원본, outline 제거) / on(원본 + outline 추가)
    Material[][] matsOff;   // 하이라이트 끔
    Material[][] matsOn;    // 하이라이트 켬

    // 이 컴포넌트 전용 런타임 복제본(공유 영향 방지)
    Material runtimeOutline;

    bool built;

    void Reset() { TryAutoCollectTargets(); }
    void OnValidate() { TryAutoCollectTargets(); BuildCache(); }
    void Awake() { BuildCache(); SetHighlighted(false); }
    void OnDisable() { ResetToOff(); }
    void OnDestroy() { ResetToOff(true); SafeDestroyRuntime(); }

    // 에디터에서 우클릭 메뉴로 재빌드
    [ContextMenu("Rebuild Outline Cache")]
    void BuildCache()
    {
        built = false;

        // 타겟 자동 수집
        if ((targets == null || targets.Length == 0) && includeChildrenIfEmpty)
            targets = GetComponentsInChildren<Renderer>(true);

        if (targets == null || targets.Length == 0) return;
        if (!outlineMaterial) return;

        // 런타임 전용 복제본(이 컴포넌트만 사용)
        if (!runtimeOutline || runtimeOutline.shader != outlineMaterial.shader)
        {
            SafeDestroyRuntime();
            runtimeOutline = new Material(outlineMaterial) { name = outlineMaterial.name + " (Runtime)" };
        }

        matsOff = new Material[targets.Length][];
        matsOn = new Material[targets.Length][];

        for (int i = 0; i < targets.Length; i++)
        {
            var r = targets[i];
            if (!r) continue;

            // 원본(shared)에서 outline 제거 → off
            var orig = r.sharedMaterials ?? System.Array.Empty<Material>();
            var offList = new List<Material>(orig);
            // 혹시 미리 붙어 있던 동일 머티 제거
            offList.RemoveAll(m => m == outlineMaterial || m == runtimeOutline);
            matsOff[i] = offList.ToArray();

            // on = off + runtimeOutline(맨 뒤)
            var onList = new List<Material>(matsOff[i]);
            onList.Add(runtimeOutline);
            matsOn[i] = onList.ToArray();
        }

        built = true;
    }

    public void SetHighlighted(bool on)
    {
        if (!built) BuildCache();
        if (!built || targets == null || matsOff == null || matsOn == null) return;

        for (int i = 0; i < targets.Length; i++)
        {
            var r = targets[i];
            if (!r) continue;
            // 런타임 인스턴스에만 적용(에셋 안전)
            r.materials = on ? matsOn[i] : matsOff[i];
        }
    }

    void ResetToOff(bool evenIfNotBuilt = false)
    {
        if (targets == null) return;
        if (!built && !evenIfNotBuilt) return;
        if (matsOff == null) return;

        for (int i = 0; i < targets.Length; i++)
        {
            var r = targets[i];
            if (!r) continue;
            r.materials = matsOff[i] ?? r.sharedMaterials;
        }
    }

    void SafeDestroyRuntime()
    {
        if (runtimeOutline)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(runtimeOutline);
            else Destroy(runtimeOutline);
#else
            Destroy(runtimeOutline);
#endif
            runtimeOutline = null;
        }
    }

    void TryAutoCollectTargets()
    {
        if (includeChildrenIfEmpty && (targets == null || targets.Length == 0))
            targets = GetComponentsInChildren<Renderer>(true);
    }
}
