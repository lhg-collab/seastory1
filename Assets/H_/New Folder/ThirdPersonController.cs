using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] float walkSpeed = 4f;          // 걷기 속도
    [SerializeField] float runSpeed = 7.5f;         // 달리기 속도
    [SerializeField] float rotationSmoothTime = 0.08f; // 회전 부드러움
    [SerializeField] float speedSmoothTime = 0.1f;     // 가감속 부드러움

    [Header("점프/중력 설정")]
    [SerializeField] float jumpHeight = 1.6f;       // 점프 높이(미터)
    [SerializeField] float gravity = -9.81f;        // 중력 값(음수)

    [Header("지면 체크")]
    [SerializeField] Transform groundCheck;         // 발 위치 빈 오브젝트 할당
    [SerializeField] float groundRadius = 0.3f;     // 접지 판정 반경
    [SerializeField] LayerMask groundMask;          // Ground 레이어 설정

    [Header("카메라")]
    [SerializeField] Transform cameraTransform;     // 비워두면 자동으로 Main Camera 사용

    CharacterController controller;

    float currentSpeed;                 // 현재 수평 속도(가감속 적용)
    float speedSmoothVelocity;          // 가감속 보조 변수
    float rotationVelocity;             // 회전 보조 변수
    Vector3 verticalVelocity;           // y축 속도만 따로 관리

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (groundCheck == null)
        {
            Debug.LogWarning("[ThirdPersonController] groundCheck가 비어있습니다. 접지 판정이 부정확할 수 있어요.");
        }
    }

    void Update()
    {
        // --- 입력 ---
        float h = Input.GetAxisRaw("Horizontal"); // A/D or ←/→
        float v = Input.GetAxisRaw("Vertical");   // W/S or ↑/↓
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift);
        bool wantsToJump = Input.GetKeyDown(KeyCode.Space);

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        bool hasInput = inputDir.sqrMagnitude > 0.0f;

        // --- 접지 판정 ---
        bool isGrounded = false;
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask, QueryTriggerInteraction.Ignore);
        else
            isGrounded = controller.isGrounded; // 예비용

        if (isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = -2f; // 지면에 붙여주는 최소 하강속도

        // --- 카메라 기준 회전/이동 방향 ---
        if (hasInput)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
            if (cameraTransform != null) targetAngle += cameraTransform.eulerAngles.y;

            float smoothedAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, targetAngle,
                ref rotationVelocity, rotationSmoothTime
            );
            transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
        }

        // --- 속도(걷기/달리기) ---
        float targetSpeed = (wantsToRun ? runSpeed : walkSpeed) * (hasInput ? 1f : 0f);
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);

        // 실제 이동 방향(카메라 기준 전방)
        Vector3 moveDir;
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
            Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();
            moveDir = (camForward * v + camRight * h).normalized;
        }
        else
        {
            moveDir = inputDir;
        }

        // --- 점프 ---
        if (wantsToJump && isGrounded)
        {
            // v = sqrt(h * -2g)
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // --- 중력 ---
        verticalVelocity.y += gravity * Time.deltaTime;

        // --- 이동 적용 ---
        Vector3 horizontal = moveDir * currentSpeed;
        Vector3 totalVelocity = new Vector3(horizontal.x, verticalVelocity.y, horizontal.z);

        controller.Move(totalVelocity * Time.deltaTime);
    }

    // 에디터에서 접지 체크 반경 시각화(선택 사항)
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
