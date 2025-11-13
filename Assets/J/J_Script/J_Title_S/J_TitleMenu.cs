using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class J_TitleMenu : MonoBehaviour
{
    [Header("Start 버튼 설정")]
    [SerializeField] string nextSceneName = "Main";   // 이동할 씬 이름 (Build Settings에 등록!)

    [SerializeField] float startDelay = 0f;           // 클릭 후 딜레이(없으면 0)
    bool loading;

    // 게임 시작 버튼에 연결
    public void StartGame()
    {
        if (loading) return;
        loading = true;

        Time.timeScale = 1f; // 혹시 멈춰있었을 대비
        if (startDelay > 0f)
            StartCoroutine(LoadAfterDelay());
        else
            SceneManager.LoadScene("J_Shop");
    }

    IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSecondsRealtime(startDelay);
        SceneManager.LoadScene("J_Shop");
    }
    // 게임 종료 버튼에 연결
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터에선 플레이 정지
#else
        Application.Quit(); // 빌드 실행 파일에서는 정상 종료
#endif
    }
}
