using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices.WindowsRuntime;
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

    string[] tutorialSteps =
    {
         "WASD로 이동하세요",
         "마우스를 움직여 시점을 변경하세요",
         "Space로 점프하세요",
         "E 또는 마우스 좌클릭으로 채집하세요",
         "바다로 들어가 탐험하세요.",
         "튜로리얼 완료!",
    };

    void Start()
    {
        tutorialCompleted= false;

        Debug.Log(" === 인벤토리 디벅 시작 ===");
        if(inventoryPanel != null)
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

        ShowTutorialStep(0);

        //SettingsManager 자동찾기
        if(settingsManager == null)
        {
            settingsManager = FindObjectOfType<SettingsManager>();
        }
    }

    void Update()
    {
        if (tutorialCompleted)
            return;
        //튜토리얼이 활성화 되어 있을 때만 작동
        if (tutorialPanel != null && tutorialPanel.activeSelf && !tutorialCompleted)
       {
            HandleEscSkip(); // ESC 스킵 처리
            CheckTutorialProgress();
        }
        // 일시정지 중이면 게임 로직 실행 안 함
        if (settingsManager != null && settingsManager.IsPaused())
        {
            return;
        }
        // 매 프레임마다 숨 감소
        //DecreaseBreath(breathDecreaseRate * Time.deltaTime);

    }

    // 튜토리얼 자동 진행 체크
    void CheckTutorialProgress()
    {
        switch(tutorialStep)
        {
            case 0: // "WASD로 이동하세요"
                if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                    Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
                {
                    NextTutorialStep();
                }
                break;

            case 1: // "마우스를 움직여 시점을 변경하세요"
                if(Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f ||
                    Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f)
                {
                    NextTutorialStep();
                }
                break;

                case 2: // "Space로 점프하세요"
                if(Input.GetKeyDown(KeyCode.Space))
                {
                    NextTutorialStep();
                }
                break;

                case 3: // "E 또는 마우스 좌클릭으로 채집하세요",
                if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.E))
                {
                    NextTutorialStep();
                }
                break;

                case 4: //  "바다에 뛰어 드세요",
                if (Input.GetKeyDown(KeyCode.LeftControl))
                {
                    NextTutorialStep();
                }
                break;

                case 5: // "튜로리얼 완료!"
                if (hasCollected)
                {
                    Debug.Log("채집 완료! 다음 단계로");
                    NextTutorialStep();
                    hasCollected = false ; // 리셋
                }
                break;
        }

    }
    void HandleEscSkip()
    {
        if (Input.GetKey(KeyCode.Backspace))
        {
            Debug.Log("Backspace 누르는 중! 시간 :" + escHoldTime);
            // Esc 누르고 있으면 시간 증가
            escHoldTime += Time.deltaTime;

            // 진행 바 업데이트
            if (skipProgressBar != null)
            {
                Debug.Log("진행 바 업데이트 :" + (escHoldTime / escRequiredTime));
                if (!skipProgressBar.gameObject.activeSelf)
                {
                    skipProgressBar.gameObject.SetActive(true);
                    Debug.Log("진행 바 표시!");
                }
                skipProgressBar.fillAmount = escHoldTime / escRequiredTime;
            }
            else
            {
                Debug.LogError("skipProgressBar가 null입니다!");
            }

            // 3초 되면 스킵
            if (escHoldTime >= escRequiredTime)
            {
                Debug.Log("ESC 3초 누름! 튜토리얼 스킵");
                SkipTutorial();
            }
        }
        else
        {
            // ESC 떼면 리셋
            if (escHoldTime > 0f)
            {
                Debug.Log("Backspace 뗌! 리셋");
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
        // 🔒 완료 후 재표시 방지 가드
        if (tutorialCompleted)
            return;

        // (기존 로직 아래 유지)
        if (step >= tutorialSteps.Length)
        {
            EndTutorial();
            return;
        }

        tutorialStep = step;

        if (tutorialDescription != null)
        {
            tutorialDescription.text = tutorialSteps[step];
            if (!tutorialDescription.gameObject.activeSelf)
                tutorialDescription.gameObject.SetActive(true);
        }

        if (tutorialPanel != null && !tutorialPanel.activeSelf)
            tutorialPanel.SetActive(true);
    }

    public void NextTutorialStep()
    {
        ShowTutorialStep(tutorialStep + 1);
    }
    public void AdvanceIfStep(int step)
    {
        // 튜토리얼이 진행 중이고, 현재 단계가 step일 때만 다음 단계로
        if (!tutorialCompleted && tutorialStep == step)
            NextTutorialStep();
    }

    public void SkipTutorial()
    {
        EndTutorial();
    }

    void EndTutorial()
    {
        {
            tutorialCompleted = true;

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
    }

    // 외부에서 미션 텍스트를 설정할 수 있는 메서드
    // public void SetMissionText(string text)
    //{
    // if(missionText != null)
    //{
    // missionText.text = text;
    //}
    //}
    public void OnItemCollected()
    {
        if(tutorialStep == 5 && !tutorialCompleted)
        {
            hasCollected = true;
            Debug.Log("채집 감지됨!");
        }
    }
}