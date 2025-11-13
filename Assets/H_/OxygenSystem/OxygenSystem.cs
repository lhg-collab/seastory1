using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OxygenSystem : MonoBehaviour
{
    [Header("Oxygen")]
    [SerializeField] float maxOxygen = 100f;
    [SerializeField] float drainPerSecond = 15f;   // 물속에서 감소
    [SerializeField] float regenPerSecond = 40f;   // 물 밖에서 회복
    [SerializeField] float lowOxyThreshold = 0.2f; // 20% 이하면 경고색(게이지 색)

    [Header("Drowning (O2=0)")]
    [SerializeField] float drowningDamagePerSecond = 10f;

    [Header("일시정지 연동(선택)")]
    [SerializeField] SettingsManager settingsManager;

    [Header("UI (자동 바인딩)")]
    [SerializeField] Image breathFill;        // Filled 게이지
    [SerializeField] Text breathInnerText;   // 게이지 안쪽 Text(자식 Text 자동 탐색)
    [SerializeField] Gradient breathColor;

    [Header("머리 위치(자동 찾기)")]
    [SerializeField] Transform headTarget;          // 기본: 메인카메라 or 아바타 Head 본
    [SerializeField] Vector3 headLocalOffset = new Vector3(0f, 0.1f, 0f); // 머리 위로 약간

    [Header("물 트리거 설정")]
    [SerializeField] string[] waterTags = { "Water", "UnderWater", "WaterVolume" };

    public float CurrentOxygen { get; private set; }
    public bool IsUnderwater { get; private set; }

    // 내부
    readonly HashSet<Collider> _waterColliders = new HashSet<Collider>();
    SimpleHealth _health;

    // 자동 바인딩 후보 이름
    static readonly string[] BreathFillNames = { "BreathFill", "Breath", "OxygenFill", "Oxygen" };
    static readonly string[] BreathLabelNames = { "BreathLabel", "OxygenLabel" };

    void Awake()
    {
        _health = GetComponentInParent<SimpleHealth>() ?? GetComponent<SimpleHealth>();

        // 그라데이션 기본값
        if (breathColor == null || breathColor.colorKeys.Length == 0)
        {
            var g = new Gradient();
            g.colorKeys = new[]
            {
                new GradientColorKey(new Color(0.2f, 0.9f, 0.9f), 0f),
                new GradientColorKey(Color.yellow, 0.5f),
                new GradientColorKey(new Color(1f, 0.35f, 0.35f), 1f),
            };
            g.alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
            breathColor = g;
        }

        TryAutoBindUI();
        TryAutoBindHead();

        CurrentOxygen = maxOxygen;
        IsUnderwater = false;
        UpdateBreathUI();
    }

    void Update()
    {
        if (settingsManager && settingsManager.IsPaused()) return;

        IsUnderwater = IsHeadInsideAnyWater();

        float dt = Time.deltaTime;
        if (IsUnderwater)
        {
            CurrentOxygen = Mathf.Max(0f, CurrentOxygen - drainPerSecond * dt);
            if (CurrentOxygen <= 0f && _health)
                _health.ApplyDamage(drowningDamagePerSecond * dt);
        }
        else
        {
            CurrentOxygen = Mathf.Min(maxOxygen, CurrentOxygen + regenPerSecond * dt);
        }

        UpdateBreathUI();
    }

    // --- 물 트리거 진입/이탈 관리 ---
    void OnTriggerEnter(Collider other)
    {
        if (HasWaterTag(other)) _waterColliders.Add(other);
    }
    void OnTriggerExit(Collider other)
    {
        if (HasWaterTag(other)) _waterColliders.Remove(other);
    }
    bool HasWaterTag(Collider c) => waterTags.Any(t => c.CompareTag(t));

    // --- 머리가 물볼륨 안에 있는지 ---
    bool IsHeadInsideAnyWater()
    {
        if (_waterColliders.Count == 0) return false;

        Vector3 headPos = GetHeadWorldPos();

        foreach (var col in _waterColliders.ToArray())
        {
            if (!col) { _waterColliders.Remove(col); continue; }

            // BoxCollider: 회전 포함 정확 판정(지역좌표로 계산)
            if (col is BoxCollider box)
            {
                Vector3 local = box.transform.InverseTransformPoint(headPos) - box.center;
                Vector3 half = box.size * 0.5f;
                if (Mathf.Abs(local.x) <= half.x &&
                    Mathf.Abs(local.y) <= half.y &&
                    Mathf.Abs(local.z) <= half.z)
                    return true;
            }
            else
            {
                // 그 외 콜라이더: AABB 근사
                if (col.bounds.Contains(headPos))
                    return true;
            }
        }
        return false;
    }

    Vector3 GetHeadWorldPos()
    {
        if (headTarget)
            return headTarget.TransformPoint(headLocalOffset);

        return transform.position + Vector3.up * 1.6f; // 최후 보정(대략 성인 키)
    }

    // --- UI ---
    void UpdateBreathUI()
    {
        float r = (maxOxygen <= 0f) ? 0f : CurrentOxygen / maxOxygen;

        if (breathFill)
        {
            if (breathFill.type != Image.Type.Filled) breathFill.type = Image.Type.Filled;
            breathFill.fillAmount = r;
            float t = Mathf.InverseLerp(1f, lowOxyThreshold, r); // r 낮을수록 t↑
            breathFill.color = breathColor.Evaluate(1f - t);
            breathFill.raycastTarget = false;

            if (!breathInnerText)
                breathInnerText = breathFill.GetComponentsInChildren<Text>(true).FirstOrDefault();
        }
        if (breathInnerText)
        {
            int cur = Mathf.RoundToInt(CurrentOxygen);
            int max = Mathf.RoundToInt(maxOxygen);
            breathInnerText.text = $"{cur}/{max}";
        }
    }

    void TryAutoBindUI()
    {
        if (!breathFill)
            breathFill = FindImageByNames(BreathFillNames);

        if (!breathFill)
        {
            foreach (var lbl in BreathLabelNames)
            {
                var go = GameObject.Find(lbl);
                if (!go) continue;
                var img = go.GetComponentsInChildren<Image>(true)
                            .FirstOrDefault(i => i.name.ToLower().Contains("fill"));
                if (img) { breathFill = img; break; }
            }
        }

        if (!breathInnerText && breathFill)
            breathInnerText = breathFill.GetComponentsInChildren<Text>(true).FirstOrDefault();
    }

    void TryAutoBindHead()
    {
        if (headTarget) return;

        // 1) 메인 카메라 우선
        if (Camera.main) { headTarget = Camera.main.transform; return; }

        // 2) 휴머노이드 아바타 Head 본
        var anim = GetComponentInParent<Animator>() ?? GetComponent<Animator>();
        if (anim && anim.isHuman)
        {
            var head = anim.GetBoneTransform(HumanBodyBones.Head);
            if (head) { headTarget = head; return; }
        }

        // 3) 이름으로 후보 찾기
        var names = new[] { "Head", "PlayerCameraRoot", "H_MainCamera", "CameraRoot" };
        foreach (var n in names)
        {
            var t = transform.root.GetComponentsInChildren<Transform>(true)
                                  .FirstOrDefault(x => x.name == n);
            if (t) { headTarget = t; return; }
        }
    }

    Image FindImageByNames(string[] names)
    {
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go)
            {
                var img = go.GetComponent<Image>();
                if (img) return img;
            }
        }
        return null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 머리 샘플 포인트
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(GetHeadWorldPos(), 0.04f);

        // 감지 중인 물 트리거 표시(윤곽)
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f);
        foreach (var c in _waterColliders)
        {
            if (!c) continue;
            Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
        }
    }
#endif
}
