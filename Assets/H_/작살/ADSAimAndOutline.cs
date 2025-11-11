using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

[DisallowMultipleComponent]
public class ADSAimAndOutline : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineVirtualCamera thirdPersonCam;   // 기존 3인칭 vcam
    public CinemachineVirtualCamera firstPersonCam;   // 1인칭 vcam(머리/카메라 위치)
    public Camera cam;                                // 레이 소스(비워두면 Camera.main)

    [Header("UI")]
    public GameObject reticleUI;                      // 에임 UI(캔버스 안 이미지)

    [Header("Aim / Interact")]
    public float gatherDistance = 3f;                 // 채집 가능 거리
    public LayerMask interactMask = ~0;               // 레이캐스트 마스크
    public bool includeTriggers = true;               // Trigger도 맞게

    [Header("Options")]
    public bool holdToAim = true;                     // 우클릭 누르고 있는 동안만 조준

    Transform _player;                                // 거리 체크용(보통 플레이어 루트)
    bool _isAiming;
    GameObject _highlightRoot;                        // 현재 하이라이트 중인 루트
    List<Renderer> _cachedRends = new List<Renderer>();
    Behaviour _cachedOutline;                         // 외부 Outline 컴포넌트(있으면)

    void Awake()
    {
        _player = transform;
        if (!cam) cam = Camera.main;
        if (reticleUI) reticleUI.SetActive(false);

        // 기본 우선순위(3인칭 > 1인칭). 조준 중에만 1인칭을 올림
        if (thirdPersonCam) thirdPersonCam.Priority = 10;
        if (firstPersonCam) firstPersonCam.Priority = 5;
    }

    void Update()
    {
        // 우클릭으로 조준 상태
        bool aimPressed = Mouse.current?.rightButton?.isPressed ?? false;
        bool nextAim = holdToAim ? aimPressed : ToggleLogic(aimPressed);

        if (nextAim != _isAiming)
        {
            _isAiming = nextAim;
            ApplyAimState(_isAiming);
        }

        // 조준 중일 때만 중앙 레이로 대상 탐색 + 하이라이트
        if (_isAiming && cam)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var qti = includeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

            if (Physics.Raycast(ray, out var hit, 1000f, interactMask, qti))
            {
                var g = hit.collider.GetComponentInParent<Gatherable>();
                float dist = Vector3.Distance(_player.position, hit.point);
                if (g && dist <= gatherDistance)
                {
                    SetHighlight(g.gameObject, true);
                }
                else ClearHighlight();
            }
            else ClearHighlight();
        }
        else
        {
            ClearHighlight();
        }
    }

    // ---------- Aim 전환 ----------
    void ApplyAimState(bool on)
    {
        if (reticleUI) reticleUI.SetActive(on);

        if (firstPersonCam && thirdPersonCam)
        {
            if (on)
            {
                firstPersonCam.Priority = 50;
                thirdPersonCam.Priority = 10;
            }
            else
            {
                firstPersonCam.Priority = 5;
                thirdPersonCam.Priority = 50;
            }
        }
    }

    bool _toggleArmed; // 토글 모드용 내부 상태
    bool ToggleLogic(bool pressedNow)
    {
        if (pressedNow && !_toggleArmed) { _toggleArmed = true; return !_isAiming; }
        if (!pressedNow && _toggleArmed) _toggleArmed = false;
        return _isAiming;
    }

    // ---------- Highlight ----------
    void SetHighlight(GameObject root, bool on)
    {
        if (_highlightRoot == root && on) return;

        // 이전 대상 해제
        if (_highlightRoot && _highlightRoot != root) ClearHighlight();

        _highlightRoot = root;

        // 1) 외부 Outline 컴포넌트가 있으면 그걸 켠다 (QuickOutline/cakeslice 등 클래스타입명이 보통 "Outline")
        _cachedOutline = FindOutlineBehaviour(root);
        if (_cachedOutline != null)
        {
            _cachedOutline.enabled = on;
            return;
        }

        // 2) 없으면 Emission 발광으로 대체
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

        if (_cachedOutline != null)
        {
            _cachedOutline.enabled = false;
        }
        else if (_cachedRends != null && _cachedRends.Count > 0)
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
        // 자식 포함 모든 컴포넌트 순회해서 타입명이 "Outline"인 Behaviour 찾기
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
