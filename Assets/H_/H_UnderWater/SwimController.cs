using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

[RequireComponent(typeof(CharacterController))]
public class SwimController : MonoBehaviour
{
    [Header("References")]
    public CharacterController characterController;
    public ThirdPersonController thirdPersonController;   // Starter Assets TPS
    public Transform cameraTransform;                     // 비우면 Camera.main

    [Header("Cinemachine (수영 중 카메라 회전)")]
    public GameObject CinemachineCameraTarget;            // PlayerCameraRoot
    public float TopClamp = 70f;
    public float BottomClamp = -30f;
    public float CameraAngleOverride = 0f;
    public bool LockCameraPosition = false;

    [Header("Swim Movement")]
    public float swimSpeed = 5f;          // 전방/좌우/상하 모두에 적용되는 3D 속도
    public float acceleration = 10f;      // 가감속(부드러움)
    public float waterDrag = 3f;          // 물 저항
    public float turnLerp = 10f;          // 수평 회전 부드러움
    public float neutralBuoyancy = 0f;    // 자연 부력(원치 않으면 0 권장)

    [Header("Surface Clamp")]
    public bool lockBelowSurface = true;  // 수면 위로 못 올라가게 막기
    [Range(0f, 1f)] public float headOffsetRatio = 0.4f;

    [Header("Debug")]
    public bool isSwimming;

    // 내부 상태
    private Vector3 _velocity;
    private InputAction _moveAction;      // WASD / 스틱
    private WaterVolume _currentWater;
    private int _waterOverlapCount;

    // 카메라 회전용
    private StarterAssetsInputs _saInput;
#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private float _yaw, _pitch;
    private const float _threshold = 0.01f;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
            return true;
#endif
        }
    }

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        _saInput = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#endif
        if (!CinemachineCameraTarget && thirdPersonController)
            CinemachineCameraTarget = thirdPersonController.CinemachineCameraTarget;

        // WASD/스틱 입력
        _moveAction = new InputAction("SwimMove", InputActionType.Value, null);
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        _moveAction.AddBinding("<Gamepad>/leftStick");

        if (CinemachineCameraTarget)
        {
            var e = CinemachineCameraTarget.transform.rotation.eulerAngles;
            _yaw = e.y; _pitch = e.x;
        }
    }

    private void OnEnable()
    {
        _moveAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
    }

    // ── WaterVolume에서 호출
    public void EnterWater(WaterVolume water)
    {
        _waterOverlapCount++;
        _currentWater = water;
        if (isSwimming) return;

        isSwimming = true;
        _velocity = Vector3.zero;

        if (thirdPersonController) thirdPersonController.enabled = false;

        if (!CinemachineCameraTarget && thirdPersonController)
            CinemachineCameraTarget = thirdPersonController.CinemachineCameraTarget;

        if (CinemachineCameraTarget)
        {
            var e = CinemachineCameraTarget.transform.rotation.eulerAngles;
            _yaw = e.y; _pitch = Mathf.Clamp(e.x, BottomClamp, TopClamp);
        }
    }

    public void ExitWater(WaterVolume water)
    {
        _waterOverlapCount = Mathf.Max(0, _waterOverlapCount - 1);
        if (_waterOverlapCount > 0) return;

        isSwimming = false;
        _currentWater = null;

        if (thirdPersonController) thirdPersonController.enabled = true;
    }

    private void Update()
    {
        if (!isSwimming) return;

        // 1) 입력 읽기 (WASD)
        Vector2 move = _moveAction.ReadValue<Vector2>();

        // 2) 카메라 전/우 벡터 (피치 포함! => 위/아래 이동 가능)
        Vector3 camFwd = cameraTransform ? cameraTransform.forward : transform.forward;
        Vector3 camRight = cameraTransform ? cameraTransform.right : transform.right;

        // 3) 목표 방향(정규화)과 목표 속도
        Vector3 desiredDir = (camFwd * move.y + camRight * move.x);
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        Vector3 targetVel = desiredDir * swimSpeed;

        // 4) (옵션) 자연 부력
        if (Mathf.Abs(neutralBuoyancy) > 0.0001f)
            targetVel += Vector3.up * neutralBuoyancy;

        // 5) 가감속 + 물 저항
        _velocity = Vector3.Lerp(_velocity, targetVel, 1f - Mathf.Exp(-acceleration * Time.deltaTime));
        _velocity = Vector3.Lerp(_velocity, Vector3.zero, 1f - Mathf.Exp(-waterDrag * Time.deltaTime));

        // 6) 이동
        characterController.Move(_velocity * Time.deltaTime);

        // 7) 회전(캐릭터는 수평 방향만 바라봄)
        Vector3 yawDir = new Vector3(_velocity.x, 0f, _velocity.z);
        if (yawDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(yawDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                1f - Mathf.Exp(-turnLerp * Time.deltaTime));
        }

        // 8) 수면 고정(원하면 끄기)
        if (lockBelowSurface && _currentWater)
        {
            float surfaceY = _currentWater.SurfaceY;
            float headOffset = characterController.height * headOffsetRatio;
            if (transform.position.y > surfaceY - headOffset)
            {
                transform.position = new Vector3(transform.position.x, surfaceY - headOffset, transform.position.z);
                // 수면에 닿았을 땐 위로 가는 속도만 제거
                if (_velocity.y > 0f) _velocity.y = 0f;
            }
        }
    }

    private void LateUpdate()
    {
        if (!isSwimming || !CinemachineCameraTarget || _saInput == null) return;

        if (_saInput.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            float dtMul = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
            _yaw += _saInput.look.x * dtMul;
            _pitch += _saInput.look.y * dtMul;
        }

        _yaw = ClampAngle(_yaw, float.MinValue, float.MaxValue);
        _pitch = ClampAngle(_pitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation =
            Quaternion.Euler(_pitch + CameraAngleOverride, _yaw, 0f);
    }

    private static float ClampAngle(float a, float min, float max)
    {
        if (a < -360f) a += 360f;
        if (a > 360f) a -= 360f;
        return Mathf.Clamp(a, min, max);
    }
}
