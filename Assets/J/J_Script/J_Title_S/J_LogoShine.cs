using UnityEngine;
using System.Collections;

public class LogoShine : MonoBehaviour
{
    public RectTransform shine;     // Shine Rect
    public RectTransform area;      // LogoGroup Rect (mask 영역)
    public float sweepTime = 0.8f;
    public float delay = 0.0f;

    public void Play() => StartCoroutine(DoShine());

    IEnumerator DoShine()
    {
        yield return new WaitForSecondsRealtime(delay);

        // 시작/끝 위치 계산: 왼쪽 바깥 → 오른쪽 바깥
        float startX = -area.rect.width * 0.7f;
        float endX = area.rect.width * 0.7f;

        Vector2 p = shine.anchoredPosition;
        p.x = startX; shine.anchoredPosition = p;
        shine.gameObject.SetActive(true);

        float t = 0;
        while (t < sweepTime)
        {
            t += Time.unscaledDeltaTime;
            p.x = Mathf.Lerp(startX, endX, t / sweepTime);
            shine.anchoredPosition = p;
            yield return null;
        }
        shine.gameObject.SetActive(false);
    }
}
