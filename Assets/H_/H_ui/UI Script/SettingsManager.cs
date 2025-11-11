using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SettingsManager : MonoBehaviour
{
    [Header("=== 설정창 === ")]
    public GameObject settingsPanel;
    public GameObject soundSettingsPanel;
    public GameObject inventoryPanel; // 인벤토리(선택)
    public Button resumeButton;
    public Button restartButton;
    public Button settingsButton;
    public Button mainmenuButton;

    [Header("=== 소리 설정 UI ===")]
    public Slider volumeSlider;
    public Toggle muteToggle;
    public Button closeButton;

    [SerializeField] string mainMenuSceneName = "TitleScene";

    static bool IsSceneInBuild(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(sceneName, name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ===== 자동 바인딩 옵션 =====
    [Header("=== 자동 바인딩 설정 ===")]
    public bool autoBindOnAwake = true;
    public bool includeInactiveUI = true; // 비활성까지 탐색

    public string[] settingsPanelNames = { "SettingsPanel", "PausePanel", "UI_Settings" };
    public string[] soundPanelNames = { "SoundSettingsPanel", "AudioPanel", "UI_Sound" };
    public string[] inventoryPanelNames = { "InventoryPanel", "UI_Inventory", "Inventory" };

    public string[] resumeBtnNames = { "BtnResume", "ResumeButton" };
    public string[] restartBtnNames = { "BtnRestart", "RestartButton" };
    public string[] settingsBtnNames = { "BtnSettings", "SettingsButton" };
    public string[] mainmenuBtnNames = { "BtnMainMenu", "MainMenuButton" };
    public string[] closeBtnNames = { "BtnClose", "CloseButton" };

    public string[] volumeSliderNames = { "SliderVolume", "VolumeSlider" };
    public string[] muteToggleNames = { "ToggleMute", "MuteToggle" };

    bool isPaused = false;
    string currentSceneName;

    // ================== 라이프사이클 ==================
    void Awake()
    {
        if (autoBindOnAwake) AutoBindUI();
        WireUpListeners(); // (초기) 중복 제거 후 연결
        EnsureEventSystem(); // 최소 1개의 EventSystem 보장
    }

    void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;

        settingsPanel?.SetActive(false);
        soundSettingsPanel?.SetActive(false);

        LoadSettings();
        // (aim/gather 관련 가시성 제어 제거)
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleSettings();
    }

    // ================== 핵심 동작 ==================
    void ToggleSettings()
    {
        // 열기/닫기 전에 “현재 보이는 패널”로 다시 잡기 + 내부 버튼 재바인딩
        if (isPaused == false) // 열릴 때
        {
            settingsPanel = FindTopmostPanel(settingsPanelNames) ?? settingsPanel;
            RebindSettingsButtons(settingsPanel);
        }

        isPaused = !isPaused;
        settingsPanel?.SetActive(isPaused);
        Time.timeScale = isPaused ? 0 : 1;

        if (isPaused)
        {
            EnsureEventSystem();
            EnsurePanelClickable(settingsPanel, bringToFront: true);

            soundSettingsPanel?.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[Settings] Open");
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("[Settings] Close");
        }
    }

    public void ResumeGame()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();

        Debug.Log("[Settings] Resume clicked");
        isPaused = false;
        settingsPanel?.SetActive(false);
        Time.timeScale = 1;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartGame()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();

        Debug.Log("[Settings] Restart clicked");
        Time.timeScale = 1;
        settingsPanel?.SetActive(false); // null-safe 보강
        isPaused = false;
        SceneManager.LoadScene(currentSceneName);
    }

    public void GoToMainMenu()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();

        Debug.Log("[Settings] MainMenu clicked");
        Time.timeScale = 1;

        if (!IsSceneInBuild(mainMenuSceneName))
        {
            Debug.LogError($"[Settings] Scene '{mainMenuSceneName}' is not in Build Settings. " +
                           "File → Build Settings… 에서 씬을 추가하거나, mainMenuSceneName을 실제 파일명으로 바꿔주세요.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OpenSoundSettings()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();

        // “현재 보이는 소리 패널”을 다시 찾아서 내부 UI 재바인딩
        soundSettingsPanel = FindTopmostPanel(soundPanelNames) ?? soundSettingsPanel;
        RebindSoundButtons(soundSettingsPanel);

        soundSettingsPanel?.SetActive(true);

        EnsureEventSystem();
        EnsurePanelClickable(soundSettingsPanel, bringToFront: true);

        Debug.Log("[Settings] Sound panel open");
    }

    public void CloseSoundSettings()
    {
        Debug.Log("[Settings] Sound panel close clicked");

        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();

        soundSettingsPanel?.SetActive(false);
    }

    public void ChangeVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
    }

    public void ChangeMute(bool isMuted)
    {
        AudioListener.pause = isMuted;
        PlayerPrefs.SetInt("Mute", isMuted ? 1 : 0);
    }

    void LoadSettings()
    {
        if (PlayerPrefs.HasKey("Volume") && volumeSlider)
        {
            float vol = PlayerPrefs.GetFloat("Volume");
            volumeSlider.value = vol;
            AudioListener.volume = vol;
        }
        if (PlayerPrefs.HasKey("Mute") && muteToggle)
        {
            bool mute = PlayerPrefs.GetInt("Mute") == 1;
            muteToggle.isOn = mute;
            AudioListener.pause = mute;
        }
    }

    public bool IsPaused() => isPaused;

    // ================== 자동 바인딩 ==================
    public void AutoBindUI()
    {
        TryFindPanel(ref settingsPanel, settingsPanelNames);
        TryFindPanel(ref soundSettingsPanel, soundPanelNames);
        TryFindPanel(ref inventoryPanel, inventoryPanelNames);

        TryFindInPanel(ref resumeButton, resumeBtnNames, settingsPanel);
        TryFindInPanel(ref restartButton, restartBtnNames, settingsPanel);
        TryFindInPanel(ref settingsButton, settingsBtnNames, settingsPanel);
        TryFindInPanel(ref mainmenuButton, mainmenuBtnNames, settingsPanel);

        TryFindInPanel(ref closeButton, closeBtnNames, soundSettingsPanel);
        TryFindInPanel(ref volumeSlider, volumeSliderNames, soundSettingsPanel);
        TryFindInPanel(ref muteToggle, muteToggleNames, soundSettingsPanel);
    }

    void WireUpListeners()
    {
        RebindSettingsButtons(settingsPanel);
        RebindSoundButtons(soundSettingsPanel);
    }

    // ================== Rebind (가장 위 패널의 실제 버튼에 연결) ==================
    void RebindSettingsButtons(GameObject panel)
    {
        TryFindInPanel(ref resumeButton, resumeBtnNames, panel);
        TryFindInPanel(ref restartButton, restartBtnNames, panel);
        TryFindInPanel(ref settingsButton, settingsBtnNames, panel);
        TryFindInPanel(ref mainmenuButton, mainmenuBtnNames, panel);

        if (resumeButton) { resumeButton.onClick.RemoveAllListeners(); resumeButton.onClick.AddListener(ResumeGame); }
        if (restartButton) { restartButton.onClick.RemoveAllListeners(); restartButton.onClick.AddListener(RestartGame); }
        if (settingsButton) { settingsButton.onClick.RemoveAllListeners(); settingsButton.onClick.AddListener(OpenSoundSettings); }
        if (mainmenuButton) { mainmenuButton.onClick.RemoveAllListeners(); mainmenuButton.onClick.AddListener(GoToMainMenu); }
    }

    void RebindSoundButtons(GameObject panel)
    {
        TryFindInPanel(ref closeButton, closeBtnNames, panel);
        TryFindInPanel(ref volumeSlider, volumeSliderNames, panel);
        TryFindInPanel(ref muteToggle, muteToggleNames, panel);

        if (closeButton) { closeButton.onClick.RemoveAllListeners(); closeButton.onClick.AddListener(CloseSoundSettings); }
        if (volumeSlider) { volumeSlider.onValueChanged.RemoveAllListeners(); volumeSlider.onValueChanged.AddListener(ChangeVolume); }
        if (muteToggle) { muteToggle.onValueChanged.RemoveAllListeners(); muteToggle.onValueChanged.AddListener(ChangeMute); }
    }

    // ================== Finder helpers ==================
    GameObject FindTopmostPanel(string[] names)
    {
        GameObject best = null; int bestOrder = int.MinValue;

        var candidates = names
            .SelectMany(n => Resources.FindObjectsOfTypeAll<Transform>()
                                      .Where(t => t && t.gameObject.scene.IsValid() &&
                                                  t.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.gameObject)
            .Distinct()
            .ToArray();

        foreach (var go in candidates)
        {
            var canvas = go.GetComponentInParent<Canvas>(true);
            if (!canvas) continue;
            var order = canvas.sortingOrder;
            if (order > bestOrder) { bestOrder = order; best = go; }
        }
        return best;
    }

    void TryFindPanel(ref GameObject panel, string[] nameCandidates)
    {
        if (panel) return;

        var t = FindByNames(nameCandidates);
        if (!t)
        {
            foreach (var c in GetAllCanvases(includeInactiveUI))
            {
                t = FindInChildrenByNames(c.transform, nameCandidates, true);
                if (t) break;
            }
        }
        if (t) panel = t.gameObject;
    }

    void TryFindInPanel<T>(ref T comp, string[] nameCandidates, GameObject panel) where T : Component
    {
        if (comp) return;

        if (panel)
        {
            var t = FindInChildrenByNames(panel.transform, nameCandidates, true);
            if (t)
            {
                var byName = t.GetComponent<T>();
                if (byName) { comp = byName; return; }
            }
            var byType = panel.GetComponentsInChildren<T>(true).FirstOrDefault();
            if (byType) { comp = byType; return; }
        }
        var global = FindByNames(nameCandidates);
        if (global)
        {
            var g = global.GetComponent<T>();
            if (g) comp = g;
        }
    }

    Transform FindByNames(string[] names)
    {
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go) return go.transform;
        }
        foreach (var c in GetAllCanvases(includeInactiveUI))
        {
            var t = FindInChildrenByNames(c.transform, names, true);
            if (t) return t;
        }
        return null;
    }

    Transform FindInChildrenByNames(Transform root, string[] names, bool includeInactive)
    {
        if (!root) return null;
        var all = root.GetComponentsInChildren<Transform>(includeInactive);
        foreach (var n in names)
        {
            var t = all.FirstOrDefault(x => string.Equals(x.name, n, System.StringComparison.OrdinalIgnoreCase));
            if (t) return t;
        }
        return null;
    }

    Canvas[] GetAllCanvases(bool includeInactive)
    {
        var all = Resources.FindObjectsOfTypeAll<Canvas>();
        if (!includeInactive)
            return FindObjectsOfType<Canvas>();
        return all.Where(c => c && c.gameObject.scene.IsValid()).ToArray();
    }

    // ================== 클릭 보장 유틸 ==================
    void EnsureEventSystem()
    {
        if (!EventSystem.current)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    void EnsurePanelClickable(GameObject panel, bool bringToFront)
    {
        if (!panel) return;

        var canvas = panel.GetComponentInParent<Canvas>(true);
        if (!canvas)
        {
            canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        var ray = canvas.GetComponent<GraphicRaycaster>();
        if (!ray) canvas.gameObject.AddComponent<GraphicRaycaster>();

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;

        foreach (var cg in panel.GetComponentsInParent<CanvasGroup>(true))
        {
            cg.alpha = Mathf.Max(0.0001f, cg.alpha);
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        // 페이드/블로커가 클릭 차단 중이면 해제
        var groups = canvas.GetComponentsInChildren<CanvasGroup>(true);
        foreach (var cg in groups)
        {
            if (panel == cg.gameObject) continue;
            var n = cg.name.ToLower();
            if ((n.Contains("fader") || n.Contains("fade") || n.Contains("blocker")) && cg.alpha < 0.01f)
                cg.blocksRaycasts = false;
        }

        if (bringToFront) BringToFront(canvas);
    }

    void BringToFront(Canvas canvas)
    {
        if (!canvas) return;
        var all = GetAllCanvases(true);
        int maxOrder = all.Length > 0 ? all.Max(c => c.sortingOrder) : 0;
        canvas.sortingOrder = maxOrder + 10;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Bind (Editor)")]
    void Editor_AutoBind()
    {
        AutoBindUI();
        WireUpListeners();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("DBG/Dump UI Under Pointer")]
    void DBG_DumpUIUnderPointer()
    {
        if (!EventSystem.current) { Debug.Log("No EventSystem"); return; }
        var ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, hits);
        Debug.Log($"[UI Raycast] count={hits.Count}");
        foreach (var h in hits)
            Debug.Log($" - {h.gameObject.name} (sortingOrder={h.sortingOrder}) path={GetPath(h.gameObject.transform)}");
    }
    string GetPath(Transform t) { var s = t.name; while (t.parent) { t = t.parent; s = t.parent.name + "/" + s; } return s; }
#endif
}
