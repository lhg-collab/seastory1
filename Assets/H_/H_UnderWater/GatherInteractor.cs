using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

public class GatherInteractor : MonoBehaviour
{
    public Camera cam;
    public float interactDistance = 3.0f;
    public LayerMask interactMask = ~0;
    public bool hitTriggers = true;
    public Transform selfRoot;
    public bool ignoreSelf = true;
    public bool offsetFromCamera = true;

    OutlineTarget _currentOutline;

    // ▼ 채집 애니메이션 / 지연 시점 설정
    [Header("Gather Timing")]
    public GatherAnimationPlayer gatherAnim;          // 플레이어 쪽에 붙은 컴포넌트
    [Range(0.5f, 1f)] public float despawnAtPercent = 0.9f;  // 세그먼트(60프레임)의 몇 % 지점에서 수집 실행할지
    public bool blockInputWhileGathering = true;      // 지연 중 중복 입력 방지

    // ▼ 소프트 락(StarterAssets 입력 무효화)
    [Header("WASD Lock (soft)")]
    public bool pauseWASDWhileGathering = true;
    public bool fallbackResetAxes = true;
    Component _sai;   // StarterAssets.StarterAssetsInputs (있으면 자동 탐지)

    // ▼ 하드락(위치/회전 고정)
    [Header("Hard Lock (brute-force)")]
    public bool hardFreezePositionWhileGathering = true;
    public bool alsoFreezeRotation = false;
    GatherFreeze _freezeComp;

    // ▼ 이동 스크립트 직접 비활성 (SwimController 등)
    [Header("Disable movement scripts while gathering")]
    [Tooltip("자동으로 SwimController를 찾고, 여기에 수동으로 다른 이동 스크립트도 넣을 수 있어요.")]
    public Behaviour[] extraMovementComponents;
    [SerializeField] string[] autoDetectTypeNames = { "SwimController" }; // 필요시 "ThirdPersonController" 등 추가

    bool _gatherBusy;
    bool _gatherHoldActive;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!selfRoot) selfRoot = transform.root;
        if (!gatherAnim) gatherAnim = GetComponentInParent<GatherAnimationPlayer>();

        _sai = FindCompInParentsByTypeName("StarterAssets.StarterAssetsInputs");

        // 자동 탐지: 부모 체인에서 지정한 타입명과 일치하는 Behaviour를 extraMovementComponents에 합치기
        var found = autoDetectTypeNames
            .SelectMany(n => FindBehavioursInParentsByTypeName(n))
            .Distinct()
            .ToArray();
        if (found.Length > 0)
        {
            extraMovementComponents = (extraMovementComponents ?? Array.Empty<Behaviour>())
                .Concat(found)
                .Distinct()
                .ToArray();
        }
    }

    void Update()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (offsetFromCamera) ray.origin += ray.direction * 0.1f;

        UpdateAimHighlight(ray);

        if (InputGate.PortalInputCaptured) return;

        // 소프트 락: 채집 지연 구간 동안 입력 무력화
        if (_gatherHoldActive && pauseWASDWhileGathering)
        {
            if (_sai)
            {
                SetVector2(_sai, "move", Vector2.zero);
                SetBool(_sai, "sprint", false);
                SetBool(_sai, "jump", false);
                //SetVector2(_sai, "look", Vector2.zero);
            }
            else if (fallbackResetAxes)
            {
                Input.ResetInputAxes();
            }
        }

        if (_gatherBusy) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            var hits = Physics.RaycastAll(ray, interactDistance, interactMask,
                hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (ignoreSelf && h.collider.transform.root == selfRoot) continue;
                var g = h.collider.GetComponentInParent<Gatherable>();
                if (g)
                {
                    float delay = 0.1f;

                    if (gatherAnim && gatherAnim.gatherClip)
                    {
                        float frameRate = Mathf.Max(1f, gatherAnim.gatherClip.frameRate);
                        int frames = (gatherAnim.framesToPlay > 0) ? gatherAnim.framesToPlay : 60;
                        float segment = frames / frameRate;

                        delay = segment * Mathf.Clamp01(despawnAtPercent);
                        gatherAnim.PlayFirstFrames();
                    }

                    _gatherHoldActive = true;
                    if (blockInputWhileGathering) _gatherBusy = true;

                    // ★ 이동 스크립트 OFF
                    SetMovementEnabled(false);

                    // ★ 하드락 시작(선택)
                    if (hardFreezePositionWhileGathering && selfRoot)
                    {
                        _freezeComp = selfRoot.gameObject.AddComponent<GatherFreeze>();
                        _freezeComp.Init(selfRoot, alsoFreezeRotation);
                    }

                    StartCoroutine(CoDelayedCollect(g, transform, delay));
                    break;
                }
            }
        }
    }

    System.Collections.IEnumerator CoDelayedCollect(Gatherable g, Transform by, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (g) g.TryCollect(by);

        _gatherHoldActive = false;
        _gatherBusy = false;

        // 이동 스크립트 ON
        SetMovementEnabled(true);

        // 하드락 해제
        if (_freezeComp) { Destroy(_freezeComp); _freezeComp = null; }
    }

    void SetMovementEnabled(bool enabled)
    {
        if (extraMovementComponents == null) return;
        foreach (var b in extraMovementComponents)
            if (b) b.enabled = enabled;

        // 물리도 중립화(끄는 순간 튕김 방지)
        if (!enabled && selfRoot)
        {
            var rb = selfRoot.GetComponent<Rigidbody>();
            if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        }
    }

    void UpdateAimHighlight(Ray ray)
    {
        OutlineTarget next = null;

        var hits = Physics.RaycastAll(ray, interactDistance, interactMask,
            hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (ignoreSelf && h.collider.transform.root == selfRoot) continue;

                var g = h.collider.GetComponentInParent<Gatherable>();
                if (!g) continue;

                next = g.GetComponent<OutlineTarget>();
                if (!next) next = g.gameObject.AddComponent<OutlineTarget>();
                break;
            }
        }

        if (_currentOutline == next) return;
        if (_currentOutline) _currentOutline.SetHighlighted(false);
        _currentOutline = next;
        if (_currentOutline) _currentOutline.SetHighlighted(true);
    }

    // ======= 리플렉션 헬퍼 =======
    Component FindCompInParentsByTypeName(string fullTypeName)
    {
        var t = AppDomain.CurrentDomain.GetAssemblies()
                 .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                 .FirstOrDefault(x => x.FullName == fullTypeName);
        if (t == null) return null;
        return GetComponentInParent(t);
    }
    Behaviour[] FindBehavioursInParentsByTypeName(string typeName)
    {
        var t = AppDomain.CurrentDomain.GetAssemblies()
                 .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                 .FirstOrDefault(x => x.Name == typeName || x.FullName == typeName);
        if (t == null || !typeof(Behaviour).IsAssignableFrom(t)) return Array.Empty<Behaviour>();
        return GetComponentsInParent(t, true).OfType<Behaviour>().ToArray();
    }

    static void SetBool(object obj, string name, bool v)
    {
        if (obj == null) return;
        var t = obj.GetType();
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (p != null && p.CanWrite && p.PropertyType == typeof(bool)) { p.SetValue(obj, v); return; }
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(bool)) { f.SetValue(obj, v); return; }
    }
    static void SetVector2(object obj, string name, Vector2 v)
    {
        if (obj == null) return;
        var t = obj.GetType();
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (p != null && p.CanWrite && p.PropertyType == typeof(Vector2)) { p.SetValue(obj, v); return; }
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(Vector2)) { f.SetValue(obj, v); return; }
    }
}

/// 임시 하드 고정 컴포넌트
public class GatherFreeze : MonoBehaviour
{
    Transform _t;
    Vector3 _pos;
    Quaternion _rot;
    bool _freezeRot;

    Rigidbody _rb;
    bool _hasRb;
    RigidbodyConstraints _origConstraints;

    public void Init(Transform target, bool freezeRotation)
    {
        _t = target;
        _pos = target.position;
        _rot = target.rotation;
        _freezeRot = freezeRotation;

        _rb = target.GetComponent<Rigidbody>();
        if (_rb)
        {
            _hasRb = true;
            _origConstraints = _rb.constraints;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.constraints = RigidbodyConstraints.FreezePositionX
                            | RigidbodyConstraints.FreezePositionY
                            | RigidbodyConstraints.FreezePositionZ
                            | (_freezeRot ? RigidbodyConstraints.FreezeRotation : 0);
        }
    }

    void LateUpdate()
    {
        if (!_t) { Destroy(this); return; }
        if (_t.position != _pos) _t.position = _pos;
        if (_freezeRot && _t.rotation != _rot) _t.rotation = _rot;

        if (_hasRb)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    void OnDestroy()
    {
        if (_hasRb && _rb) _rb.constraints = _origConstraints;
    }
}
