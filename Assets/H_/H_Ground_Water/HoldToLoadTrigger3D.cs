using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class HoldToLoadTrigger3D : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string sceneToLoad;
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;
    [SerializeField] private string requiredTag = "Player";

    [Header("Hold & Fade")]
    [SerializeField, Min(0f)] private float holdDuration = 2f; // 연속 접촉 필요 시간(초)
    [SerializeField] private CanvasGroup fadeCanvasGroup;       // 전체 화면 검은 Image에 CanvasGroup(초기 alpha=0)
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private bool fadeBackInAfterLoad = true;   // 다음 씬에서 다시 밝아지게

    private bool isLoading;
    private float holdTimer;
    private Coroutine holdRoutine;

    // 동일 태그의 여러 콜라이더(자식 포함) 대응
    private readonly HashSet<Collider> _contacts = new HashSet<Collider>();

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true; // 편의 설정
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLoading || !other.CompareTag(requiredTag)) return;

        // 첫 진입 시 타이머 시작
        if (_contacts.Add(other) && _contacts.Count == 1 && holdRoutine == null)
        {
            holdRoutine = StartCoroutine(HoldWatcher());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;

        _contacts.Remove(other);

        // 완전히 벗어나면 타이머 리셋 & 코루틴 중지
        if (_contacts.Count == 0)
        {
            holdTimer = 0f;
            if (holdRoutine != null)
            {
                StopCoroutine(holdRoutine);
                holdRoutine = null;
            }
        }
    }

    private IEnumerator HoldWatcher()
    {
        holdTimer = 0f;

        // 연속으로 holdDuration만큼 유지되어야 성공
        while (_contacts.Count > 0 && holdTimer < holdDuration)
        {
            holdTimer += Time.unscaledDeltaTime; // 타임스케일과 무관
            yield return null;
        }

        holdRoutine = null;

        if (_contacts.Count == 0 || isLoading) yield break; // 중간에 끊겼거나 이미 로딩 중

        // 조건 충족 → 페이드 후 로드
        yield return StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        isLoading = true;

        if (fadeCanvasGroup && fadeBackInAfterLoad)
            DontDestroyOnLoad(fadeCanvasGroup.gameObject);

        yield return StartCoroutine(FadeTo(1f, fadeDuration)); // 어둡게

        var op = SceneManager.LoadSceneAsync(sceneToLoad, loadMode);
        while (!op.isDone) yield return null;

        if (fadeCanvasGroup && fadeBackInAfterLoad)
        {
            yield return StartCoroutine(FadeTo(0f, fadeDuration)); // 밝게
            Destroy(fadeCanvasGroup.gameObject);
        }
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        if (!fadeCanvasGroup || duration <= 0f) yield break;

        fadeCanvasGroup.blocksRaycasts = true; // 입력 차단
        float start = fadeCanvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = target;
    }
}
