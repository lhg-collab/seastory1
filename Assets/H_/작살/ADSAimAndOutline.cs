using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

[DisallowMultipleComponent]
public class ADSAimAndOutline : MonoBehaviour, ITargetProvider
{
    [Header("Cameras (3rd-person only)")]
    public CinemachineVirtualCamera thirdPersonCam;
    public Camera cam;                                // 비우면 Camera.main
    public bool forceBrainLateUpdate = true;

    [Header("Aim / Interact")]
    public float gatherDistance = 3f;                 // 사거리(하이라이트 허용 범위)
    public LayerMask interactMask = ~0;
    public bool includeTriggers = true;

    [Header("Division-style ADS")]
    public float normalFOV = 60f;
    public float aimFOV = 45f;
    public float normalDistance = 4.5f;               // 3rdPersonFollow CameraDistance
    public float aimDistance = 2.2f;
    public Vector3 normalShoulder = new Vector3(0.5f, 0f, 0f);
    public Vector3 aimShoulder = new Vector3(0.8f, 0.1f, 0f);
    public float camBlendLerp = 12f;

    [Header("Assist")]
    public float aimAssistRadius = 0.15f;             // 중앙 보정 스피어캐스트 반경

    [Header("Self Filter")]
    public Transform selfRoot;                        // 비우면 this.transform
    Collider[] _selfCols;

    // --- ITargetProvider 공개값 ---
    public bool IsAiming => _isAiming;
    public Gatherable CurrentTarget => _currentTarget;
    public float CurrentTargetDistance => _currentTargetDist;

    // 내부 상태
    Transform _player;
    bool _isAiming;
    Gatherable _currentTarget;
    float _currentTargetDist;

    // 하이라이트 캐시
    GameObject _highlightRoot;
    List<Renderer> _cachedRends = new List<Renderer>();
    Behaviour _cachedOutline;

    // 시네머신 캐시
    Cinemachine3rdPersonFollow _tpf;
    CinemachineBrain _brain;

    void Awake()
    {
        _player = transform;
        if (!cam) cam = Camera.main;

        if (!selfRoot) selfRoot = transform;
        _selfCols = selfRoot.GetComponentsInChildren<Collider>(true);

        if (thirdPersonCam)
        {
            _tpf = thirdPersonCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            thirdPersonCam.m_Lens.FieldOfView = normalFOV;
            if (_tpf) { _tpf.CameraDistance = normalDistance; _tpf.ShoulderOffset = normalShoulder; }
        }

        if (cam)
        {
            _brain = cam.GetComponent<CinemachineBrain>();
            if (_brain && forceBrainLateUpdate)
                _brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
        }
    }

    void Update()
    {
        // 우클릭 유지형 조준
        _isAiming = Mouse.current?.rightButton?.isPressed ?? false;

        // 카메라 파라미터 보간(디비전 느낌)
        if (thirdPersonCam)
        {
            float targetFov = _isAiming ? aimFOV : normalFOV;
            thirdPersonCam.m_Lens.FieldOfView =
                Mathf.Lerp(thirdPersonCam.m_Lens.FieldOfView, targetFov, Time.deltaTime * camBlendLerp);

            if (_tpf)
            {
                _tpf.CameraDistance =
                    Mathf.Lerp(_tpf.CameraDistance, _isAiming ? aimDistance : normalDistance, Time.deltaTime * camBlendLerp);
                _tpf.ShoulderOffset =
                    Vector3.Lerp(_tpf.ShoulderOffset, _isAiming ? aimShoulder : normalShoulder, Time.deltaTime * camBlendLerp);
            }
        }
    }

    // 카메라가 LateUpdate에서 최종 위치가 정해지므로, 스캔/하이라이트는 여기서
    void LateUpdate() => UpdateTargetAndHighlight();

    void UpdateTargetAndHighlight()
    {
        if (!_isAiming || !cam)
        {
            _currentTarget = null;
            ClearHighlight();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var qti = includeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        bool found = false;
        RaycastHit hit = default;

        // 1) 스피어캐스트(보정)
        if (aimAssistRadius > 0f)
        {
            var hits = Physics.SphereCastAll(ray, aimAssistRadius, 1000f, interactMask, qti);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsSelf(hits[i].collider)) continue;
                hit = hits[i]; found = true; break;
            }
        }
        // 2) 실패 시 레이캐스트
        if (!found)
        {
            var hits = Physics.RaycastAll(ray, 1000f, interactMask, qti);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsSelf(hits[i].collider)) continue;
                hit = hits[i]; found = true; break;
            }
        }

        if (found)
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

    bool IsSelf(Collider c)
    {
        if (!c) return false;
        for (int i = 0; i < _selfCols.Length; i++) if (_selfCols[i] == c) return true;
        return c.transform.root == selfRoot;
    }

    // 외부에서 강제로 하이라이트 해제하고 싶을 때 사용
    public void ForceClearHighlight() => ClearHighlight();

    // ----- Highlight -----
    void SetHighlight(GameObject root, bool on)
    {
        if (_highlightRoot == root && on) return;
        if (_highlightRoot && _highlightRoot != root) ClearHighlight();
        _highlightRoot = root;

        _cachedOutline = FindOutline(root);
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

    Behaviour FindOutline(GameObject root)
    {
        var comps = root.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var b = comps[i] as Behaviour;
            if (b != null && b.GetType().Name == "Outline")
                return b;
        }
        return null;
    }
}
