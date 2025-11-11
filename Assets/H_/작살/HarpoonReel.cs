using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class HarpoonReel : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                       // 비우면 Camera.main
    public Transform muzzle;                 // 로프 시작점(총구/손)
    public Transform reelAnchor;             // 끌어올 기준점(보통 손/가슴)

    [Header("Target Provider (ADS)")]
    public MonoBehaviour targetProviderSource; // ADSAimAndOutline 드래그
    public bool requireAim = true;
    public bool useAimTarget = true;
    ITargetProvider _provider;

    [Header("Hit Settings (fallback ray)")]
    public float shootDistance = 40f;
    public LayerMask hitMask = ~0;
    public bool hitTriggers = true;

    [Header("Reel Physics (spring)")]
    public bool useSpring = true;            // 스프링 탄성 사용 여부
    public float springK = 40f;              // 스프링 강도
    public float springDamping = 8f;         // 감쇠(속도 저항)
    public float reelInRate = 10f;           // 초당 로프 길이 줄이기(m/s)
    public float stopDistance = 1.3f;        // 수집 판정 거리(ClosestPoint 기준)
    public float maxHookTime = 5f;           // 안전 타임아웃

    [Header("Rope (auto & curve)")]
    public LineRenderer rope;                // 비워두면 자동 생성
    public bool autoCreateRope = true;
    public float ropeWidth = 0.03f;
    public Color ropeColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
    public Material ropeMaterialOverride;    // 비우면 기본 Unlit 머티리얼 생성
    [Range(4, 128)] public int ropeSegments = 24; // 곡선 샘플 개수
    [Range(0f, 1f)] public float ropeSag = 0.12f;  // 중력에 의한 처짐 비율(길이에 비례)
    public float whipInfluence = 0.25f;      // 대상의 횡방향 속도에 따른 휘두름

    [Header("Rope Texture Tiling")]
    public bool tileTexture = true;
    public float tilesPerMeter = 1.0f;       // 길이 1m 당 타일 반복 수

    [Header("Self Filter")]
    public Transform selfRoot;               // 비우면 this.transform
    Collider[] _selfCols;

    // --- state (hook) ---
    bool _busy;
    Transform _hooked;
    Rigidbody _hookedRb;
    FishSwim _hookedAI;
    Gatherable _hookedGatherable;
    Collider _hookedCol;

    // spring state
    float _currentRopeLen;
    Vector3 _vel;            // 내부 시뮬레이션용 속도(kinematic일 때)
    Vector3 _prevHookPos;    // 휘두름 계산용 이전 프레임 위치

    Coroutine _co;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!muzzle && cam) muzzle = cam.transform;
        if (!reelAnchor) reelAnchor = muzzle ? muzzle : transform;

        if (!selfRoot) selfRoot = transform;
        _selfCols = selfRoot.GetComponentsInChildren<Collider>(true);

        // Provider 바인딩
        _provider = targetProviderSource as ITargetProvider;
        if (_provider == null) _provider = GetComponentInParent<ITargetProvider>();

        EnsureRope(); // 자동 생성/세팅
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;
        EnsureRope();
    }

    void EnsureRope()
    {
        if (!autoCreateRope) return;

        if (!rope)
        {
            var go = new GameObject("Rope (auto)");
            go.transform.SetParent(transform, false);
            rope = go.AddComponent<LineRenderer>();
        }

        rope.enabled = false;
        rope.positionCount = Mathf.Max(2, ropeSegments);
        rope.useWorldSpace = true;
        rope.alignment = LineAlignment.View;
        rope.numCornerVertices = 6;
        rope.numCapVertices = 6;
        rope.textureMode = tileTexture ? LineTextureMode.Tile : LineTextureMode.Stretch;
        rope.startWidth = rope.endWidth = Mathf.Max(0.001f, ropeWidth);
        rope.startColor = rope.endColor = ropeColor;

        var r = rope.GetComponent<Renderer>();
        if (r)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = 5;
        }

        if (ropeMaterialOverride) rope.material = ropeMaterialOverride;
        else
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (!sh) sh = Shader.Find("HDRP/Unlit");
            if (!sh) sh = Shader.Find("Unlit/Color");
            if (!sh) sh = Shader.Find("Sprites/Default");
            if (sh)
            {
                var mat = new Material(sh);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ropeColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", ropeColor);
                rope.material = mat;
            }
        }
    }

    void Update()
    {
        bool fire = (Mouse.current?.leftButton?.wasPressedThisFrame ?? false) ||
                    (Keyboard.current?.eKey?.wasPressedThisFrame ?? false);
        if (!fire || _busy) return;
        if (requireAim && _provider != null && !_provider.IsAiming) return;

        TryFire();
    }

    void TryFire()
    {
        // 1) ADS 타깃 우선
        if (useAimTarget && _provider != null && _provider.CurrentTarget != null)
        {
            HookTo(_provider.CurrentTarget.transform);
            return;
        }

        // 2) 직접 레이
        var qti = hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var hits = Physics.RaycastAll(ray, shootDistance, hitMask, qti);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsSelf(hits[i].collider)) continue;
            var g = hits[i].collider.GetComponentInParent<Gatherable>();
            if (!g) continue;
            HookTo(g.transform, hits[i].collider);
            return;
        }
    }

    void HookTo(Transform target, Collider hitCol = null)
    {
        _hookedGatherable = target.GetComponentInParent<Gatherable>();
        if (!_hookedGatherable) return;

        _hooked = _hookedGatherable.transform;
        _hookedCol = hitCol ? hitCol : _hooked.GetComponentInChildren<Collider>();
        _hookedRb = _hooked.GetComponent<Rigidbody>();
        _hookedAI = _hooked.GetComponent<FishSwim>();
        if (_hookedAI) _hookedAI.enabled = false;

        if (_hookedRb)
        {
            _hookedRb.useGravity = false;
            _hookedRb.velocity = Vector3.zero;
            _hookedRb.angularVelocity = Vector3.zero;
            _hookedRb.isKinematic = true; // 우리 쪽에서 위치 갱신
        }

        // 초기 로프 길이 = 현재 거리
        Vector3 anchor = reelAnchor ? reelAnchor.position : transform.position;
        _currentRopeLen = Vector3.Distance(anchor, _hooked.position);
        _currentRopeLen = Mathf.Max(_currentRopeLen, stopDistance);
        _vel = Vector3.zero;
        _prevHookPos = _hooked.position;

        if (rope) { rope.enabled = true; rope.positionCount = Mathf.Max(2, ropeSegments); }

        _busy = true;
        _co = StartCoroutine(ReelRoutine());
    }

    IEnumerator ReelRoutine()
    {
        float t = 0f;
        while (_hooked && t < maxHookTime)
        {
            t += Time.deltaTime;

            Vector3 anchor = reelAnchor ? reelAnchor.position : transform.position;

            // 수집 거리 체크 (콜라이더 가장 가까운 점 기준)
            Vector3 closest = (_hookedCol != null)
                ? Physics.ClosestPoint(anchor, _hookedCol, _hookedCol.transform.position, _hookedCol.transform.rotation)
                : _hooked.position;

            float distToClosest = Vector3.Distance(anchor, closest);
            if (distToClosest <= stopDistance)
            {
                _hookedGatherable.TryCollect(transform);
                (targetProviderSource as ADSAimAndOutline)?.ForceClearHighlight();
                break;
            }

            // ---- 스프링 릴링 or 일정 속도 끌기 ----
            Vector3 toAnchor = anchor - _hooked.position;
            float dist = toAnchor.magnitude + 1e-5f;
            Vector3 dir = toAnchor / dist;

            if (useSpring)
            {
                // 로프 길이를 점점 줄임(릴 인)
                _currentRopeLen = Mathf.Max(stopDistance, _currentRopeLen - reelInRate * Time.deltaTime);

                // 스프링: 길이를 초과한 만큼 당기는 힘 (지나치면 0)
                float extension = Mathf.Max(0f, dist - _currentRopeLen);
                // 속도의 로프방향 성분에 감쇠
                float velAlong = Vector3.Dot(_vel, dir);
                float accel = springK * extension - springDamping * velAlong;

                _vel += accel * dir * Time.deltaTime;
                Vector3 step = _vel * Time.deltaTime;

                // overshoot 과도 방지
                if (Vector3.Dot(step, dir) > extension) step = dir * extension;

                MoveHook(_hooked.position + step);
            }
            else
            {
                // 단순 끌기(이전 방식)
                float pullSpeed = reelInRate; // 같은 값 재활용
                Vector3 step = dir * pullSpeed * Time.deltaTime;
                MoveHook(_hooked.position + step);
            }

            // 로프 비주얼 업데이트
            UpdateRopeVisual(anchor, _hooked.position, (_hooked.position - _prevHookPos) / Mathf.Max(Time.deltaTime, 1e-5f));
            _prevHookPos = _hooked.position;

            yield return null;
        }

        CleanupHook();
    }

    void MoveHook(Vector3 newPos)
    {
        if (_hookedRb && _hookedRb.isKinematic) _hookedRb.MovePosition(newPos);
        else _hooked.position = newPos;
    }

    void CleanupHook()
    {
        if (_hookedRb) _hookedRb.isKinematic = false;
        if (_hookedAI) _hookedAI.enabled = true;
        if (rope) rope.enabled = false;

        _busy = false;
        _hooked = null; _hookedRb = null; _hookedAI = null; _hookedGatherable = null; _hookedCol = null;
        _co = null;
    }

    // -------- Rope Visual (Bezier + 타일링) --------
    void UpdateRopeVisual(Vector3 a, Vector3 b, Vector3 hookVelocity)
    {
        if (!rope) return;

        int n = Mathf.Max(2, ropeSegments);
        if (rope.positionCount != n) rope.positionCount = n;

        Vector3 ab = b - a;
        float length = ab.magnitude;

        // 가운데 컨트롤 포인트 = 중점 + 처짐 + 휘두름
        Vector3 mid = a + ab * 0.5f;

        // 처짐(아래 방향) : 길이에 비례
        Vector3 sag = Vector3.down * (ropeSag * length);

        // 휘두름: 후크의 횡방향 속도를 추출하여 살짝 보태기
        Vector3 dir = (length > 1e-4f) ? (ab / length) : Vector3.forward;
        Vector3 lateralVel = hookVelocity - Vector3.Project(hookVelocity, dir);
        Vector3 whip = lateralVel * whipInfluence * 0.05f; // 스케일 다운

        Vector3 control = mid + sag + whip;

        // 단순 2차 베지어 보간 (a - control - b)
        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (float)i / (n - 1);
            Vector3 p = Bezier2(a, control, b, t);
            rope.SetPosition(i, p);
        }

        // 텍스처 타일링(지원 셰이더일 때만)
        if (tileTexture && rope.material != null)
        {
            float repeat = Mathf.Max(0.01f, length * tilesPerMeter);
            if (rope.material.HasProperty("_BaseMap"))
                rope.material.SetTextureScale("_BaseMap", new Vector2(repeat, 1f));
            else if (rope.material.HasProperty("_MainTex"))
                rope.material.SetTextureScale("_MainTex", new Vector2(repeat, 1f));
            else
            {
                // 일부 셰이더는 mainTextureScale 사용
                var tex = rope.material.mainTextureScale; // get
                rope.material.mainTextureScale = new Vector2(repeat, 1f);
            }
        }
    }

    static Vector3 Bezier2(Vector3 a, Vector3 c, Vector3 b, float t)
    {
        // 2차 베지어: Lerp(Lerp(a,c,t), Lerp(c,b,t), t)
        Vector3 p0 = Vector3.Lerp(a, c, t);
        Vector3 p1 = Vector3.Lerp(c, b, t);
        return Vector3.Lerp(p0, p1, t);
    }

    // -------- Utils --------
    bool IsSelf(Collider c)
    {
        if (!c) return false;
        for (int i = 0; i < _selfCols.Length; i++) if (_selfCols[i] == c) return true;
        return c.transform.root == selfRoot;
    }
}
