using UnityEngine;

public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [Header("대상")]
    public Transform target;                 // 플레이어 Transform
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f); // 머리 높이 정도

    [Header("오비트")]
    public float distance = 4.0f;            // 기본 카메라 거리
    public float minDistance = 1.2f;
    public float maxDistance = 6.0f;
    public float mouseSensitivityX = 320f;   // deg/s
    public float mouseSensitivityY = 320f;   // deg/s
    public float minPitch = -35f;            // 아래로
    public float maxPitch = 75f;             // 위로
    public bool requireRightMouse = false;   // 우클릭 중에만 회전할지

    [Header("충돌 처리")]
    public float collisionRadius = 0.2f;     // 스피어캐스트 반경
    public float collisionBuffer = 0.05f;    // 벽에서 살짝 띄우기
    public LayerMask obstructionMask;        // 카메라를 막는 레이어

    [Header("부드러움")]
    public float positionSmoothTime = 0.04f; // 위치 스무딩

    [Header("커서")]
    public bool lockCursorAtStart = true;    // 시작 시 커서 잠금

    float yaw;           // 수평 각
    float pitch = 15f;   // 수직 각
    float currentDistance;
    float distanceVelocity;                  // SmoothDamp용
    Vector3 currentVelocity;                 // SmoothDamp용(위치)

    void Awake()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        currentDistance = distance;
    }

    void OnEnable()
    {
        if (lockCursorAtStart) LockCursor(true);
    }

    void Update()
    {
        if (target == null) return;

        // 커서 토글
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
        if (Input.GetMouseButtonDown(0) && !Cursor.lockState.Equals(CursorLockMode.Locked)) LockCursor(true);

        // 마우스 회전
        bool allowRotate = !requireRightMouse || Input.GetMouseButton(1);
        if (allowRotate && Cursor.lockState == CursorLockMode.Locked)
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            yaw += mx * mouseSensitivityX * Time.deltaTime;
            pitch -= my * mouseSensitivityY * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // 휠 줌
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance = Mathf.Clamp(distance - scroll * 2.0f, minDistance, maxDistance);
        }

        // 목표 회전/피벗
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + targetOffset;

        // 충돌 보정 거리 계산 (스피어캐스트)
        float desiredDist = distance;
        if (Physics.SphereCast(pivot, collisionRadius, -rot * Vector3.forward, out RaycastHit hit, distance, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            desiredDist = Mathf.Clamp(hit.distance - collisionBuffer, minDistance, maxDistance);
        }

        // 거리 스무딩
        currentDistance = Mathf.SmoothDamp(currentDistance, desiredDist, ref distanceVelocity, positionSmoothTime);

        // 위치 스무딩
        Vector3 desiredPos = pivot - rot * Vector3.forward * currentDistance;
        Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, positionSmoothTime);

        transform.position = smoothedPos;
        transform.rotation = rot;

        // 피벗을 바라보게(확실한 조준)
        transform.LookAt(pivot);
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // 에디터에서 충돌 스피어 시각화
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + targetOffset;
        Vector3 camDir = -rot * Vector3.forward;
        Gizmos.DrawWireSphere(pivot + camDir * currentDistance, collisionRadius);
    }
}
