using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("=== 게임오버 UI ===")]
    public GameObject DeathPanel;
    public Button RetryButton;
    public Button MainButton;

    void Start()
    {
        // DeathPanel 초기 비활성화
        if (DeathPanel != null)
        {
            DeathPanel.SetActive(false);
        }

        if (RetryButton != null)
        {
            RetryButton.onClick.AddListener(() => {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.PlayButtonClick();
                }
                Retry();
            });
        }

        if (MainButton != null)
        {
            MainButton.onClick.AddListener(() => {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.PlayButtonClick();
                }
                Main();
            });
        }
    }

    // 게임오버 표시 (외부에서 호출)
    public void ShowGameOver()
    {
        Debug.Log("게임오버!");

        if (DeathPanel != null)
        {
            DeathPanel.SetActive(true);
        }

        // 모든 오디오 정지!
        if (AudioManager.instance != null)
        {
            AudioManager.instance.StopAllAudio();
        }

        Time.timeScale = 1;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Retry()
    {
        Time.timeScale = 1;

        // 튜토리얼 BGM 시작
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayTutorialBGM();
        }

        SceneManager.LoadScene(1);
    }

    void Main()
    {
        Time.timeScale = 1;

        // 타이틀 BGM 시작
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayTitleBGM();
        }

        SceneManager.LoadScene(0);
    }
}