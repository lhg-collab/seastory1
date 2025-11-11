using UnityEngine;

[DisallowMultipleComponent]
public class OutlineTarget : MonoBehaviour
{
    public Color outlineColor = new(1f, 0.7f, 0.2f, 1f);
    public float outlineWidth = 0.02f;
    public bool startHighlighted = false;

    static Material s_sharedMat;           // 셰어드 아웃라인 머티리얼(1개만 생성)
    Renderer[] _renderers;
    bool _on;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        EnsureMaterial();
        if (startHighlighted) SetHighlighted(true);
    }

    void EnsureMaterial()
    {
        if (s_sharedMat) return;
        var sh = Shader.Find("URP/Outline/InvertedHull");
        if (!sh) { Debug.LogError("Outline shader not found: URP/Outline/InvertedHull"); return; }
        s_sharedMat = new Material(sh) { name = "__OutlineMat(shared)" };
        s_sharedMat.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
    }

    public void SetHighlighted(bool on)
    {
        if (_on == on || _renderers == null) return;
        _on = on;

        foreach (var r in _renderers)
        {
            if (!r) continue;
            var mats = r.sharedMaterials;

            if (on)
            {
                // 이미 붙어있는지 확인
                bool exists = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] && mats[i].shader == s_sharedMat.shader) { exists = true; break; }

                if (!exists)
                {
                    var nm = new Material[mats.Length + 1];
                    System.Array.Copy(mats, nm, mats.Length);
                    nm[nm.Length - 1] = s_sharedMat;
                    r.sharedMaterials = nm;
                }

                // 색/두께는 인스턴스별 MPB로 설정
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb, r.sharedMaterials.Length - 1);
                mpb.SetColor("_OutlineColor", outlineColor);
                mpb.SetFloat("_OutlineWidth", outlineWidth);
                r.SetPropertyBlock(mpb, r.sharedMaterials.Length - 1);
            }
            else
            {
                // 아웃라인 머티리얼 제거
                int count = 0;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] && mats[i].shader == s_sharedMat.shader) continue;
                    mats[count++] = mats[i];
                }
                System.Array.Resize(ref mats, count);
                r.sharedMaterials = mats;
            }
        }
    }

    void OnDisable() { SetHighlighted(false); }
}
