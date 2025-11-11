using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

[DisallowMultipleComponent]
public class ADSAimAndOutline : MonoBehaviour
{
    [Header("Cameras (3rd-person only)")]
    public CinemachineVirtualCamera thirdPersonCam;
    public Camera cam;                                // 비우면 Camera.main
    public bool forceBrainLateUpdate = true;          // ✅ Brain을 LateUpdate로 강제

    [Header("UI")]
    public GameObject reticleUI;

    [Header("Aim / Interact")]
    public float gatherDistance = 3f;
    public LayerMask interactMask = ~0;
    public bool includeTriggers = true;
    public bool requireAimToCollect = true;

    [Header("Division-style ADS Tuning")]
    public float normalFOV = 60f;
    public float aimFOV = 45f;
    public float normalDistance = 4.5f;
    public float aimDistance = 2.2f;
    public Vector3 normalShoulder = new Vector3(0.5f, 0.0f, 0f);
    public Vector3 aimShoulder = new Vector3(0.8f, 0.1f, 0f);
    public float camBlendLerp = 12f;

    [Header("Movement while ADS (Starter Assets optional)")]
    public bool slowDownWhileADS = true;
    [Range(0.2f, 1.0f)] public float adsSpeedFactor = 0.6f;

    [Header("Hit Assist / Debug")]
    public float aimAssistRadius = 0.15f;             // ✅ 스피어캐스트 반경(미세 보정)
    public bool drawDebugRay = false;                 // 디버그 라인

    Transform _player;
    bool _isAiming;

    GameObject _highlightRoot;
    Gatherable _currentTarget;
    float _currentTargetDist;
    List<Renderer> _cachedRends = new List<Renderer>();
    Behaviour _cachedOutline;

    Cinemachine3rdPersonFollow _tpf;
    CinemachineBrain _brain;

    // Starter Assets 감속(있을 때만 적용)
    Component _tpc; System.Reflection.PropertyInfo _piMoveSpeed, _piSprintSpeed;
    float _origMoveSpeed, _origSprintSpeed; bool _adsSpeedApplied;

    void Awake()
    {
        _player = transform;
        if (!cam) cam = Camera.main;
        if (reticleUI) reticleUI.SetActive(false);

        if (thirdPersonCam)
        {
            _tpf = thirdPersonCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            thirdPersonCam.m_Lens.FieldOfView = normalFOV;
            if (_tpf) { _tpf.CameraDistance = normalDistance; _tpf.ShoulderOffset = normalShoulder; }
        }

        // ✅ Brain 업데이트 타이밍 강제 (카메라가 먼저, 스캔은 그 다음)
        if (cam)
        {
            _brain = cam.GetComponent<CinemachineBrain>();
            if (_brain && forceBrainLateUpdate)
                _brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
        }

        TryBindStarterAssetsTpc();
    }

    void Update()
    {
        // 우클릭 유지형 ADS
        bool aimPressed = Mouse.current?.rightButton?.isPressed ?? false;
        SetAimState(aimPressed);

        // 채집 입력
        bool collectPressed =
            (Mouse.current?.leftButton?.wasPressedThisFrame ?? false) ||
            (Keyboard.current?.eKey?.wasPressedThisFrame ?? false);

        if (collectPressed && _currentTarget != null)
        {
            if (!requireAimToCollect || _isAiming)
            {
                if (_currentTargetDist <= gatherDistance)
                {
                    _currentTarget.TryCollect(_player);
                    ClearHighlight();
                    _currentTarget = null;
                }
            }
        }

        // 카메라 파라미터 보간
        if (thirdPersonCam)
        {
            float targetFov = _isAiming ? aimFOV : normalFOV;
            thirdPersonCam.m_Lens.FieldOfView = Mathf.Lerp(thirdPersonCam.m_Lens.FieldOfView, targetFov, Time.deltaTime * camBlendLerp);
            if (_tpf)
            {
                _tpf.CameraDistance = Mathf.Lerp(_tpf.CameraDistance, _isAiming ? aimDistance : normalDistance, Time.deltaTime * camBlendLerp);
                _tpf.ShoulderOffset = Vector3.Lerp(_tpf.ShoulderOffset, _isAiming ? aimShoulder : normalShoulder, Time.deltaTime * camBlendLerp);
            }
        }
    }

    // ✅ 카메라가 모두 갱신된 "후"에 타겟 스캔
    void LateUpdate()
    {
        UpdateTargetAndHighlight();
    }

    void SetAimState(bool on)
    {
        if (_isAiming == on) return;
        _isAiming = on;
        if (reticleUI) reticleUI.SetActive(_isAiming);

        if (slowDownWhileADS && _tpc != null)
        {
            if (_isAiming && !_adsSpeedApplied)
            {
                _origMoveSpeed = ReadFloat(_tpc, _piMoveSpeed, 4.0f);
                _origSprintSpeed = ReadFloat(_tpc, _piSprintSpeed, 5.335f);
                WriteFloat(_tpc, _piMoveSpeed, _origMoveSpeed * adsSpeedFactor);
                WriteFloat(_tpc, _piSprintSpeed, _origSprintSpeed * adsSpeedFactor);
                _adsSpeedApplied = true;
            }
            else if (!_isAiming && _adsSpeedApplied)
            {
                WriteFloat(_tpc, _piMoveSpeed, _origMoveSpeed);
                WriteFloat(_tpc, _piSprintSpeed, _origSprintSpeed);
                _adsSpeedApplied = false;
            }
        }
    }

    // --- 타겟 탐색 & 하이라이트 (정지 상태에서도 잘 잡히게 레이 + 스피어) ---
    void UpdateTargetAndHighlight()
    {
        if (!cam) cam = Camera.main;

        if (!_isAiming || !cam)
        {
            _currentTarget = null;
            ClearHighlight();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var qti = includeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        bool hitSomething = false;
        RaycastHit hit;

        // 1) 스피어캐스트로 살짝 여유를 주고
        if (aimAssistRadius > 0f && Physics.SphereCast(ray, aimAssistRadius, out hit, 1000f, interactMask, qti))
        {
            hitSomething = true;
        }
        // 2) 못 잡으면 레이캐스트로 재시도
        else if (Physics.Raycast(ray, out hit, 1000f, interactMask, qti))
        {
            hitSomething = true;
        }

        if (drawDebugRay)
        {
            Color c = hitSomething ? Color.cyan : Color.red;
            Debug.DrawRay(ray.origin, ray.direction * (hitSomething ? hit.distance : 3f), c, 0.02f);
            if (aimAssistRadius > 0f)
                DebugExtension.DebugWireSphere(hitSomething ? hit.point : ray.origin + ray.direction * 3f, aimAssistRadius, c, 0.02f);
        }

        if (hitSomething)
        {
            var g = hit.collider.GetComponentInParent<Gatherable>();
            _currentTargetDist = Vector3.Distance(_player.position, hit.point);

            if (g && _currentTargetDist <= gatherDistance)
            {
                _currentTarget = g;
                SetHighlight(g.gameObject, true);
                return;
            }
        }

        _currentTarget = null;
        ClearHighlight();
    }

    // --- 하이라이트 ---
    void SetHighlight(GameObject root, bool on)
    {
        if (_highlightRoot == root && on) return;
        if (_highlightRoot && _highlightRoot != root) ClearHighlight();

        _highlightRoot = root;

        _cachedOutline = FindOutlineBehaviour(root);
        if (_cachedOutline != null) { _cachedOutline.enabled = on; return; }

        _cachedRends.Clear();
        root.GetComponentsInChildren(true, _cachedRends);
        foreach (var r in _cachedRends)
        {
            if (!r || !r.material) continue;
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", Color.cyan * 1.4f);
        }
    }

    void ClearHighlight()
    {
        if (!_highlightRoot) return;

        if (_cachedOutline != null) _cachedOutline.enabled = false;
        else
        {
            foreach (var r in _cachedRends)
            {
                if (!r || !r.material) continue;
                r.material.DisableKeyword("_EMISSION");
            }
        }
        _highlightRoot = null;
        _cachedOutline = null;
        _cachedRends.Clear();
    }

    Behaviour FindOutlineBehaviour(GameObject root)
    {
        var comps = root.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var b = comps[i] as Behaviour;
            if (b != null && b.GetType().Name == "Outline") return b;
        }
        return null;
    }

    // --- Starter Assets 감속 바인딩 (있을 때만) ---
    void TryBindStarterAssetsTpc()
    {
        var list = GetComponentsInParent<Component>(true);
        foreach (var c in list)
        {
            if (c == null) continue;
            if (c.GetType().Name == "ThirdPersonController")
            {
                _tpc = c;
                var t = c.GetType();
                _piMoveSpeed = t.GetProperty("MoveSpeed");
                _piSprintSpeed = t.GetProperty("SprintSpeed");
                break;
            }
        }
    }
    float ReadFloat(object obj, System.Reflection.PropertyInfo pi, float fallback) { try { return (pi != null) ? (float)pi.GetValue(obj) : fallback; } catch { return fallback; } }
    void WriteFloat(object obj, System.Reflection.PropertyInfo pi, float v) { try { if (pi != null && pi.CanWrite) pi.SetValue(obj, v); } catch { } }
}

// --- 간단 디버그 구체 그리기 유틸(선택) ---
public static class DebugExtension
{
    public static void DebugWireSphere(Vector3 pos, float radius, Color color, float duration = 0f)
    {
        const int seg = 16;
        Vector3 last = pos + new Vector3(radius, 0, 0);
        for (int i = 1; i <= seg; i++)
        {
            float a = i * (Mathf.PI * 2f / seg);
            Vector3 next = pos + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            Debug.DrawLine(last, next, color, duration);
            Debug.DrawLine(pos + new Vector3(0, Mathf.Cos(a) * radius, Mathf.Sin(a) * radius),
                           pos + new Vector3(0, Mathf.Cos(a - (Mathf.PI * 2f / seg)) * radius, Mathf.Sin(a - (Mathf.PI * 2f / seg)) * radius),
                           color, duration);
            last = next;
        }
    }
}
