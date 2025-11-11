using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    [Header("=== 버튼 ===")]
    public Button startButton;
    public Button optionButton;
    public Button quitButton;

    // Start is called before the first frame update
    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(() => {
                // 버튼 클릭 소리
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.PlayButtonClick();
                }
                StartGame();
            });
        }

        if (optionButton != null)
        {
            optionButton.onClick.AddListener(() => {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.PlayButtonClick();
                }
                OpenOptions();
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() => {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.PlayButtonClick();
                }
                QuitGame();
            });
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
    void OpenOptions()
    {
        Debug.Log("옵션 열기");
    }
    public void QuitGame()
    {
#if  UNITY_EDITOR // 유니티 에디터에서 테스트할 때 Play 모드를 종료하기 위함
        // 에디터에서 실행 중일 때
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 빌드된 게임에서 실행 중일 때
        Application.Quit(); // 실제 빌드된 게임을 종료
#endif

        Debug.Log("게임 종료!");
    }
}
