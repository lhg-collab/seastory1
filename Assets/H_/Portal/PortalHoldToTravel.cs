using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public class PortalHoldToTravel : MonoBehaviour
{
    [Header("씬 이동")]
    [SerializeField] string targetSceneName = "H_ground";
    [SerializeField] float holdSeconds = 2f;

    [Header("입력(신/구 둘 다 지원)")]
    [SerializeField] KeyCode legacyKey = KeyCode.E;
    [SerializeField] bool legacyMouseLeft = true;

#if ENABLE_INPUT_SYSTEM
    [Tooltip("Input System을 쓴다면, Interact 같은 액션을 할당하세요 (Keyboard E, Mouse Left)")]
    public InputActionReference interactAction; // Optional
#endif

    [Header("UI(선택)")]
    [SerializeField] Image progressFillImage;          // Filled Image
    [SerializeField] Canvas worldspaceCanvas;           // 월드 스페이스 캔버스
    [SerializeField] bool faceCamera = true;          // 카메라 보게 할지

    [Header("표시 위치/크기 보정")]
    [Tooltip("포탈 중심에서 카메라 방향으로 얼마나 당겨낼지(미터)")]
    [SerializeField] float billboardOffset = 0.8f;      // 🔸크게 잡아 가려짐 방지
    [Tooltip("거리와 무관하게 화면에서 비슷한 크기로 보이게")]
    [SerializeField] bool keepConstantSize = true;
    [Tooltip("카메라와 1m 떨어졌을 때의 UI 크기(스케일)")]
    [SerializeField] float sizeAt1m = 0.08f;
    [Tooltip("정렬을 강제로 올릴지(겹침 이슈 시)")]
    [SerializeField] bool forceTopSorting = true;
    [SerializeField] int sortingOrderWhenShown = 100;

    [Header("자동 바인딩 (언더워터에서만)")]
    [SerializeField] string[] underwaterSceneNames = { "H_UnderWater" };
    [SerializeField] string canvasName = "Portal_Canvas";
    [SerializeField] string fillImageName = "Portal";

    [Header("기타")]
    [SerializeField] float resetSpeed = 3f;
    [SerializeField] SettingsManager settingsManager;

    bool playerInside;
    float holdTimer;
    bool loading;

#if ENABLE_INPUT_SYSTEM
    bool isHoldingByAction;
    void OnEnable()
    {
        if (interactAction && interactAction.action != null)
        {
            interactAction.action.performed += OnActionPerformed;
            interactAction.action.canceled += OnActionCanceled;
            interactAction.action.Enable();
        }
    }
    void OnDisable()
    {
        if (interactAction && interactAction.action != null)
        {
            interactAction.action.performed -= OnActionPerformed;
            interactAction.action.canceled -= OnActionCanceled;
            interactAction.action.Disable();
        }
    }
    void OnActionPerformed(InputAction.CallbackContext ctx)
    {
        if (!playerInside || loading) return;
        isHoldingByAction = true;
    }
    void OnActionCanceled(InputAction.CallbackContext ctx)
    {
        isHoldingByAction = false;
    }
#endif

    void Awake()
    {
        TryAutoBindUI_OnlyInUnderwater();
    }

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (!playerInside || loading) return;

        // 일시정지 중이면 무시
        if (settingsManager && settingsManager.IsPaused()) return;

        // 포탈 존 우선권
        InputGate.PortalInputCaptured = true;

        // 입력 판정
        bool holding = false;
#if ENABLE_INPUT_SYSTEM
        if (interactAction && interactAction.action != null)
            holding = isHoldingByAction;
#endif
        if (!holding)
        {
            if (Input.GetKey(legacyKey)) holding = true;
            else if (legacyMouseLeft && Input.GetMouseButton(0)) holding = true;
        }

        // 진행
        if (holding) holdTimer += Time.deltaTime;
        else holdTimer = Mathf.MoveTowards(holdTimer, 0f, Time.deltaTime * resetSpeed);

        // UI 업데이트
        if (progressFillImage)
        {
            progressFillImage.raycastTarget = false;
            progressFillImage.fillAmount = Mathf.Clamp01(holdTimer / holdSeconds);
        }

        // 항상 보이는 자리로 위치/회전/크기 보정
        if (faceCamera) PlaceCanvasFacingCamera();

        if (holdTimer >= holdSeconds)
            LoadTargetScene();
    }

    void LoadTargetScene()
    {
        if (loading) return;
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[PortalHoldToTravel] targetSceneName 비어있음");
            return;
        }
        if (!IsSceneInBuild(targetSceneName))
        {
            Debug.LogError($"[PortalHoldToTravel] '{targetSceneName}' 씬이 Build Settings에 없음");
            return;
        }
        loading = true;
        SceneManager.LoadScene(targetSceneName);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 런타임 생성 대비 재바인딩 시도
        if (!worldspaceCanvas || !progressFillImage)
            TryAutoBindUI_OnlyInUnderwater();

        playerInside = true;
        holdTimer = 0f;

        if (progressFillImage) progressFillImage.fillAmount = 0f;

        if (worldspaceCanvas)
        {
            worldspaceCanvas.enabled = true;

            if (forceTopSorting)
            {
                worldspaceCanvas.overrideSorting = true;
                worldspaceCanvas.sortingOrder = sortingOrderWhenShown;
            }

            // 진입 즉시 한 번 배치
            PlaceCanvasFacingCamera();
        }

        InputGate.PortalInputCaptured = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        holdTimer = 0f;

        if (progressFillImage) progressFillImage.fillAmount = 0f;
        if (worldspaceCanvas) worldspaceCanvas.enabled = false;

        InputGate.PortalInputCaptured = false;
    }

    // --- 핵심: 카메라 정면으로 돌리고, 포탈 위치에서 카메라쪽으로 'billboardOffset' 만큼 당겨놓기 + 거리 기반 스케일 ---
    void PlaceCanvasFacingCamera()
    {
        if (!worldspaceCanvas) return;
        var cam = Camera.main;
        if (!cam) return;

        var t = worldspaceCanvas.transform;
        var pivot = transform.position;                       // 포탈 중심
        var toCam = (cam.transform.position - pivot).normalized;

        // 포탈 중심에서 카메라 방향으로 offset만큼 이동
        t.position = pivot + toCam * Mathf.Max(0.01f, billboardOffset);

        // 카메라 정면 바라보기(뒤집힘 방지)
        t.rotation = Quaternion.LookRotation(cam.transform.position - t.position);

        // 일정 크기 유지(거리 기반 스케일)
        if (keepConstantSize)
        {
            float d = Vector3.Distance(t.position, cam.transform.position);
            float s = Mathf.Max(0.0001f, d * sizeAt1m);         // d가 1m일 때 sizeAt1m
            t.localScale = Vector3.one * s;
        }
    }

    // --- Helpers ---
    void TryAutoBindUI_OnlyInUnderwater()
    {
        if (!IsUnderwaterScene()) return;

        var canvasCandidates = new[] { canvasName, "PortalCanvas", "Portal UI", "PortalUI", "Portal-Canvas" }
                               .Where(s => !string.IsNullOrEmpty(s)).ToArray();
        var fillCandidates = new[] { fillImageName, "Portal", "Progress", "HoldProgress", "Gauge" }
                               .Where(s => !string.IsNullOrEmpty(s)).ToArray();

        if (!worldspaceCanvas)
        {
            worldspaceCanvas = FindCanvas(transform, canvasCandidates, exact: true)
                               ?? FindCanvas(transform, canvasCandidates, exact: false)
                               ?? FindCanvasInScene(canvasCandidates, preferWorldSpace: true, reference: transform.position);

            if (worldspaceCanvas)
            {
                worldspaceCanvas.renderMode = RenderMode.WorldSpace;
                if (!worldspaceCanvas.worldCamera && Camera.main)
                    worldspaceCanvas.worldCamera = Camera.main;
                worldspaceCanvas.enabled = false;

                if (forceTopSorting)
                {
                    worldspaceCanvas.overrideSorting = true;
                    worldspaceCanvas.sortingOrder = sortingOrderWhenShown;
                }
            }
        }

        if (!progressFillImage && worldspaceCanvas)
        {
            progressFillImage = FindImage(worldspaceCanvas.transform, fillCandidates, exact: true)
                                ?? FindImage(worldspaceCanvas.transform, fillCandidates, exact: false)
                                ?? worldspaceCanvas.GetComponentsInChildren<Image>(true)
                                     .FirstOrDefault(i => i.type == Image.Type.Filled);

            if (progressFillImage)
            {
                if (progressFillImage.type != Image.Type.Filled)
                    progressFillImage.type = Image.Type.Filled;
                progressFillImage.fillAmount = 0f;
                progressFillImage.raycastTarget = false;
            }
        }
    }

    Canvas FindCanvas(Transform root, string[] names, bool exact)
    {
        var canvases = root.GetComponentsInChildren<Canvas>(true);
        return canvases.FirstOrDefault(c =>
        {
            var n = c.name;
            return exact ? names.Any(x => string.Equals(n, x, StringComparison.OrdinalIgnoreCase))
                         : names.Any(x => n.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
        });
    }

    Canvas FindCanvasInScene(string[] names, bool preferWorldSpace, Vector3 reference)
    {
        var canvases = GameObject.FindObjectsOfType<Canvas>(true);
        var named = canvases.Where(c =>
            names.Any(x => string.Equals(c.name, x, StringComparison.OrdinalIgnoreCase)) ||
            names.Any(x => c.name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0));

        var list = (preferWorldSpace ? named.Where(c => c.renderMode == RenderMode.WorldSpace).ToList()
                                     : named.ToList());
        if (list.Count == 0 && preferWorldSpace) list = named.ToList();

        return list.OrderBy(c => Vector3.SqrMagnitude(c.transform.position - reference)).FirstOrDefault();
    }

    Image FindImage(Transform root, string[] names, bool exact)
    {
        var imgs = root.GetComponentsInChildren<Image>(true);
        return imgs.FirstOrDefault(i =>
        {
            var n = i.name;
            return exact ? names.Any(x => string.Equals(n, x, StringComparison.OrdinalIgnoreCase))
                         : names.Any(x => n.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
        });
    }

    bool IsUnderwaterScene()
    {
        var cur = SceneManager.GetActiveScene().name;
        foreach (var n in underwaterSceneNames)
            if (!string.IsNullOrEmpty(n) &&
                string.Equals(cur, n, StringComparison.OrdinalIgnoreCase))
                return true;
        return cur.IndexOf("underwater", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsSceneInBuild(string name)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(sceneName, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

// === 전역 게이트: 채집/포탈 충돌 방지용 ===
public static class InputGate
{
    public static bool PortalInputCaptured = false;
}
