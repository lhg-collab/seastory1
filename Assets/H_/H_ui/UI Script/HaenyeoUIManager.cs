using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.SceneManager; // 씬 이름 체크용 (지금은 사용 X, 필요 없으면 using 지워도 됨)
using UnityEngine.Rendering;
using Unity.VisualScripting;

public class HaenyeoUIManager : MonoBehaviour
{
    //[Header("=== 중앙 상단 : 목표 === ")]
    //public Text missionText;

    [Header("=== 중앙 상단 : 튜토리얼 === ")]
    public GameObject tutorialPanel;
    public Text tutorialDescription;
    public Image skipProgressBar; // ESC 3초 진행 바

    [Header("=== 중앙 하단 : 인벤토리 ===")]
    public GameObject inventoryPanel;
    public GameObject inventoryBackground;

    [Header("=== 참조 === ")]
    public SettingsManager settingsManager; // 설정 매니저와 연결

    int tutorialStep = 0;

    [Header("✅ 튜토리얼 완료 처리")]
    [Tooltip("튜토리얼 완료 시 잠깐 띄울 완료 패널(없으면 건너뜀)")]
    public GameObject tutorialCompletePanel;
    [Tooltip("완료 패널을 자동으로 숨김")]
    public bool autoHideCompletePanel = true;
    [Tooltip("완료 패널 자동 숨김까지 대기 시간(실시간)")]
    public float completePanelHideDelay = 1.5f;

    // 스킵 관련
    float escHoldTime = 0f; // ESC 누른 시간
    float escRequiredTime = 2f; // 필요한 시간 (2초)
    bool tutorialCompleted = false; // 튜토리얼 완료 여부
    bool hasCollected = false; // 채집 완료했는지

    // ★ 바다 도착 플래그
    bool reachedSea = false;

    // ★ 마지막 단계 유지 시간 & 코루틴 핸들
    [Header("튜토리얼 마무리 설정")]
    [Tooltip("마지막 단계 텍스트를 유지할 시간(초)")]
    public float lastStepHoldSeconds = 2f;
    Coroutine _lastStepRoutine;

    // ★ 1초 후 다음 스텝 이동용 플래그
    bool isWaitingNextStep = false;

    string[] tutorialSteps =
    {
         "WASD로 이동 SHIFT로 달리기",
         "마우스를 움직여 시점을 변경하세요",
         "Space로 점프하세요",
         "E 또는 마우스 좌클릭으로 채집하세요",
         "상점에 채집한 해산물을 판매하세요.",
         "바다로 이동하세요.",
         "앞에 보이는 테왁에 다가가 E 또는 마우스 좌클릭으로 꾹 누르고 있으면 마을로 귀환!",
         "튜토리얼 완료! 자유롭게 돌아다니며 플레이하세요."
    };

    // ====== ★ 씬이 바뀌어도 유지되는 static 튜토리얼 상태 ======
    static bool s_initialized = false;
    static int s_savedStep = 0;
    static bool s_savedCompleted = false;
    // 필요하면 hasCollected / reachedSea 도 static 으로 빼줄 수 있음 (지금은 단계/완료만 공유)

    void Awake()
    {
        // SettingsManager 자동 찾기
        if (!settingsManager)
            settingsManager = FindObjectOfType<SettingsManager>();
    }

    void Start()
    {
        // 처음 한 번만 기본값 설정
        if (!s_initialized)
        {
            s_initialized = true;
            s_savedStep = 0;
            s_savedCompleted = false;
        }

        // static 상태 → 인스턴스 상태로 복사
        tutorialStep = s_savedStep;
        tutorialCompleted = s_savedCompleted;

        Debug.Log(" === 인벤토리 디벅 시작 ===");
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            Debug.Log("인벤토리 패널 활성화!");
        }
        else
        {
            Debug.LogError("인벤토리 패널이 연결되지 않았습니다!");
        }

        // 튜토리얼 BGM 재생
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayTutorialBGM();
            Debug.Log("튜토리얼 BGM 재생 요청!");
        }

        // 스킵 진행 바 초기화
        if (skipProgressBar != null)
        {
            skipProgressBar.fillAmount = 0f;
            skipProgressBar.gameObject.SetActive(false);
        }

        // 이미 튜토리얼 끝난 상태라면 UI만 정리하고 종료
        if (tutorialCompleted)
        {
            if (tutorialPanel) tutorialPanel.SetActive(false);
            if (tutorialDescription)
            {
                tutorialDescription.text = "";
                tutorialDescription.gameObject.SetActive(false);
            }
            if (tutorialCompletePanel) tutorialCompletePanel.SetActive(false);
            return;
        }

        // 진행 중이면 현재 단계의 텍스트를 다시 표시
        ShowTutorialStep(tutorialStep);
    }

    void Update()
    {
        if (tutorialCompleted) return;

        // 튜토리얼이 활성화 되어 있을 때만 작동
        if (tutorialPanel != null && tutorialPanel.activeSelf && !tutorialCompleted)
        {
            HandleEscSkip(); // ESC 스킵 처리
            CheckTutorialProgress();
        }

        // 일시정지 중이면 게임 로직 실행 안 함
        if (settingsManager != null && settingsManager.IsPaused())
            return;
    }

    // 튜토리얼 자동 진행 체크
    void CheckTutorialProgress()
    {
        switch (tutorialStep)
        {
            case 0: // "WASD로 이동 shift로 달리기"
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                    Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
                    NextTutorialStepDelayed(1f);
                break;

            case 1: // "마우스를 움직여 시점을 변경하세요"
                if (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f ||
                    Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f)
                    NextTutorialStepDelayed(1f);
                break;

            case 2: // "Space로 점프하세요"
                if (Input.GetKeyDown(KeyCode.Space))
                    NextTutorialStepDelayed(1f);
                break;

            case 3: // "E 또는 마우스 좌클릭으로 채집하세요"
                if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.E))
                    NextTutorialStepDelayed(1f);
                break;

            case 4: // "상점에 채집한 해산물을 판매하세요."
                // ★ 더 이상 키 입력으로 넘기지 않음.
                //    실제로 상점에서 판매가 완료되면 NotifySoldInShop()을 호출해서 진행.
                break;

            case 5: // "바다로 이동하세요."
                // ★ 키 입력 대신, H_Water에서 NotifyReachedSea() 호출 → 플래그로 진행
                if (reachedSea)
                {
                    reachedSea = false;   // 한 번만 사용
                    NextTutorialStepDelayed(1f);
                }
                break;

            case 6: // "앞에 보이는 테왁에 다가가 E 또는 마우스 좌클릭으로 꾹 누르고 있으면 마을로 귀환!"
                if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.E))
                    NextTutorialStepDelayed(1f);
                break;

            case 7: // "튜토리얼 완료! 자유롭게 돌아다니며 플레이하세요."
                // ★ 마지막 단계: 여기서는 아무 것도 안 함.
                // ShowTutorialStep(7) 에서 2초 뒤 자동으로 EndTutorial() 이 호출됨.
                break;
        }
    }

    void HandleEscSkip()
    {
        if (Input.GetKey(KeyCode.Backspace))
        {
            // Esc 누르고 있으면 시간 증가
            escHoldTime += Time.deltaTime;

            // 진행 바 업데이트
            if (skipProgressBar != null)
            {
                if (!skipProgressBar.gameObject.activeSelf)
                    skipProgressBar.gameObject.SetActive(true);
                skipProgressBar.fillAmount = escHoldTime / escRequiredTime;
            }

            // 2초 되면 스킵
            if (escHoldTime >= escRequiredTime)
            {
                Debug.Log("ESC 2초 누름! 튜토리얼 스킵");
                SkipTutorial();
            }
        }
        else
        {
            // ESC 떼면 리셋
            if (escHoldTime > 0f)
            {
                escHoldTime = 0f;
                if (skipProgressBar != null)
                {
                    skipProgressBar.fillAmount = 0f;
                    skipProgressBar.gameObject.SetActive(false);
                }
            }
        }
    }

    void ShowTutorialStep(int step)
    {
        if (tutorialCompleted) return;
        if (step >= tutorialSteps.Length)
        {
            EndTutorial();
            return;
        }

        tutorialStep = step;
        s_savedStep = tutorialStep; // ★ static 상태도 같이 갱신

        if (tutorialDescription != null)
        {
            tutorialDescription.text = tutorialSteps[step];
            if (!tutorialDescription.gameObject.activeSelf)
                tutorialDescription.gameObject.SetActive(true);
        }

        if (tutorialPanel != null && !tutorialPanel.activeSelf)
            tutorialPanel.SetActive(true);

        // ★ 마지막 단계라면, 2초 유지 후 자동으로 튜토리얼 종료
        if (tutorialStep == 7)
        {
            // 혹시 이전에 돌고 있던 코루틴이 있다면 정지
            if (_lastStepRoutine != null)
            {
                StopCoroutine(_lastStepRoutine);
                _lastStepRoutine = null;
            }
            _lastStepRoutine = StartCoroutine(CoEndTutorialAfterDelay(lastStepHoldSeconds));
        }
    }

    // ★ 마지막 단계 유지 후 종료 코루틴
    IEnumerator CoEndTutorialAfterDelay(float delay)
    {
        float t = 0f;
        while (t < delay && !tutorialCompleted)
        {
            t += Time.unscaledDeltaTime;  // 타임스케일 무시하고 실시간 기준
            yield return null;
        }
        _lastStepRoutine = null;

        if (!tutorialCompleted) // 중간에 스킵 등으로 이미 끝났으면 다시 EndTutorial() 안 부름
            EndTutorial();
    }

    // ★ 1초 후 다음 스텝으로 넘어가는 코루틴
    public void NextTutorialStepDelayed(float delay = 1f)
    {
        if (isWaitingNextStep || tutorialCompleted)
            return;

        StartCoroutine(CoNextTutorialStepAfterDelay(delay));
    }

    IEnumerator CoNextTutorialStepAfterDelay(float delay)
    {
        isWaitingNextStep = true;

        float t = 0f;
        while (t < delay && !tutorialCompleted)
        {
            t += Time.unscaledDeltaTime; // 실시간 기준으로 대기
            yield return null;
        }

        isWaitingNextStep = false;

        if (!tutorialCompleted)
            NextTutorialStep();
    }

    // 즉시 다음 스텝으로 넘기는 함수 (외부에서 호출 가능)
    public void NextTutorialStep() => ShowTutorialStep(tutorialStep + 1);

    public void AdvanceIfStep(int step)
    {
        if (!tutorialCompleted && tutorialStep == step)
            NextTutorialStep();
    }

    public void SkipTutorial() => EndTutorial();

    void EndTutorial()
    {
        tutorialCompleted = true;
        s_savedCompleted = true; // ★ static 완료 상태 저장

        // 스킵바 숨김/초기화
        if (skipProgressBar)
        {
            skipProgressBar.fillAmount = 0f;
            skipProgressBar.gameObject.SetActive(false);
        }

        // 튜토리얼 패널/텍스트 확실히 끄기
        if (tutorialPanel) tutorialPanel.SetActive(false);
        if (tutorialDescription)
        {
            tutorialDescription.text = "";
            tutorialDescription.gameObject.SetActive(false);
        }

        // (선택) 완료 패널 잠깐 보여주고 자동 숨김
        if (tutorialCompletePanel)
        {
            tutorialCompletePanel.SetActive(true);
            if (autoHideCompletePanel)
                StartCoroutine(HideCompletePanelAfterDelay());
        }

        Debug.Log("튜토리얼 완료");
    }

    IEnumerator HideCompletePanelAfterDelay()
    {
        // Time.timeScale 0이어도 동작하도록 실시간 대기
        float t = 0f;
        while (t < completePanelHideDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (tutorialCompletePanel) tutorialCompletePanel.SetActive(false);
    }

    public void OnItemCollected()
    {
        if (tutorialStep == 5 && !tutorialCompleted)
        {
            hasCollected = true;
            Debug.Log("채집 감지됨!");
        }
    }

    // ★ 상점에서 판매 완료됐을 때 호출할 함수
    public void NotifySoldInShop()
    {
        if (tutorialCompleted) return;

        Debug.Log("튜토리얼: 상점 판매 감지!");
        // 현재 스텝이 4일 때만 1초 후 다음 단계로
        if (tutorialStep == 4)
            NextTutorialStepDelayed(1f);
    }

    // ★ H_Water 트리거에서 호출할 함수
    public void NotifyReachedSea()
    {
        if (!tutorialCompleted && tutorialStep == 5)
        {
            reachedSea = true;
            Debug.Log("튜토리얼: 바다 도착 감지!");
        }
    }
}
