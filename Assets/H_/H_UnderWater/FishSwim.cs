using UnityEngine;
using System; // StringComparison 용

[DisallowMultipleComponent]
public class FishSwim : MonoBehaviour
{
    [Header("Area")]
    public BoxCollider swimBounds;        // 수중 영역(월드 좌표)
    public float boundsMargin = 2f;       // 경계에서 어느 정도는 안쪽으로 다니게

    [Header("Movement")]
    public float minSpeed = 1.3f;
    public float maxSpeed = 3.0f;
    public float turnSpeed = 3.0f;        // 회전 반응 속도
    public Vector2 changeTargetTime = new Vector2(2f, 5f);
    public float arriveDistance = 1.5f;

    [Header("Avoidance")]
    public LayerMask obstacleMask = ~0;   // 바위 등 장애물 레이어
    public float avoidRayRadius = 0.3f;
    public float avoidDistance = 3f;

    [Header("Bobbing (자연스러움)")]
    public float bobAmplitude = 0.25f;
    public float bobFrequency = 1.2f;

    [Header("Special (GAOHRI 전용)")]
    public string specialNameToken = "GAOHRI"; // 이름에 이 텍스트가 포함되면 특수 회전
    public float specialXRotation = -90f;

    Vector3 _targetPos;
    float _targetTimer;
    float _speed;
    float _bobPhase;
    bool _forceXMinus90; // GAOHRI 여부

    // ★ 씬에서 이름이 "Water"인 BoxCollider를 자동으로 찾기
    void Awake()
    {
        AutoAssignSwimBounds();
    }

    // ★ swimBounds가 비어 있으면 이름이 "Water"인 BoxCollider를 찾아서 할당
    void AutoAssignSwimBounds()
    {
        if (swimBounds) return;

        var all = FindObjectsOfType<BoxCollider>(true);
        foreach (var c in all)
        {
            if (!c) continue;
            if (string.Equals(c.name, "Water", StringComparison.OrdinalIgnoreCase))
            {
                swimBounds = c;
                Debug.Log("[FishSwim] 'Water' BoxCollider를 swimBounds로 자동 할당했습니다.", this);
                return;
            }
        }

        Debug.LogWarning("[FishSwim] 이름이 'Water'인 BoxCollider를 찾지 못했습니다. swimBounds를 수동으로 지정해주세요.", this);
    }

    void OnEnable()
    {
        // 혹시라도 Awake 전에 만들어졌거나, 씬 전환 후 다시 켜질 때를 대비해 한 번 더 시도
        if (!swimBounds)
            AutoAssignSwimBounds();

        if (!swimBounds)
        {
            // 여전히 없으면 동작하면 안 되니 바로 리턴
            Debug.LogError("[FishSwim] swimBounds 가 설정되지 않아 움직임을 비활성화합니다.", this);
            enabled = false;
            return;
        }

        _speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
        _bobPhase = UnityEngine.Random.value * 10f;
        PickNewTarget();

        // 이름에 "GAOHRI"가 포함되어 있으면 X축 -90 고정 모드
        _forceXMinus90 = gameObject.name.IndexOf(specialNameToken, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void Update()
    {
        if (!swimBounds) return;

        // 목표 갱신
        _targetTimer -= Time.deltaTime;
        if (_targetTimer <= 0f || Vector3.Distance(transform.position, _targetPos) < arriveDistance)
            PickNewTarget();

        // 기본 유도 방향
        Vector3 desiredDir = (_targetPos - transform.position);
        if (desiredDir.sqrMagnitude < 0.0001f) desiredDir = transform.forward;
        desiredDir.Normalize();

        // 경계 가까우면 안쪽으로 유도
        var bounds = swimBounds.bounds;
        if (!bounds.Contains(transform.position) ||
            DistanceToBoundsEdge(transform.position, bounds) < boundsMargin)
        {
            Vector3 towardInside = (bounds.ClosestPoint(transform.position) - transform.position).normalized;
            desiredDir = Vector3.Slerp(desiredDir, towardInside, 0.6f);
        }

        // 장애물 회피(전방 예측: "갈 방향" 기준)
        if (Physics.SphereCast(transform.position, avoidRayRadius, desiredDir,
            out var hit, avoidDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 avoidDir =
                Vector3.Reflect(desiredDir, hit.normal)
              + hit.normal * 0.5f
              + transform.right * UnityEngine.Random.Range(-0.8f, 0.8f);
            desiredDir = Vector3.Slerp(desiredDir, avoidDir.normalized, 0.8f);
        }

        // 회전/이동 방향 스무싱 (이 방향으로 실제 이동)
        Vector3 curDir = Vector3.Slerp(transform.forward, desiredDir, turnSpeed * Time.deltaTime).normalized;

        // 목표 회전 만들기
        Quaternion targetRot;
        if (_forceXMinus90)
        {
            // Yaw는 curDir에서 계산, X는 -90으로 고정, Roll은 0
            float yaw = Mathf.Atan2(curDir.x, curDir.z) * Mathf.Rad2Deg;
            targetRot = Quaternion.Euler(specialXRotation, yaw, 0f);
        }
        else
        {
            targetRot = Quaternion.LookRotation(curDir, Vector3.up);
        }

        transform.rotation = targetRot;

        // 이동 (방향은 curDir 사용)
        Vector3 move = curDir * _speed * Time.deltaTime;
        move += Vector3.up * Mathf.Sin(Time.time * bobFrequency + _bobPhase) * bobAmplitude * Time.deltaTime;
        transform.position += move;
    }

    void PickNewTarget()
    {
        if (!swimBounds) return;

        _targetTimer = UnityEngine.Random.Range(changeTargetTime.x, changeTargetTime.y);
        _speed = UnityEngine.Random.Range(minSpeed, maxSpeed);

        var b = swimBounds.bounds;
        float mx = boundsMargin, my = boundsMargin, mz = boundsMargin;
        _targetPos = new Vector3(
            UnityEngine.Random.Range(b.min.x + mx, b.max.x - mx),
            UnityEngine.Random.Range(b.min.y + my, b.max.y - my),
            UnityEngine.Random.Range(b.min.z + mz, b.max.z - mz)
        );
    }

    static float DistanceToBoundsEdge(Vector3 p, Bounds b)
    {
        Vector3 c = b.ClosestPoint(p);
        return Vector3.Distance(p, c);
    }
}
