using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class J_ScreenFader : MonoBehaviour
{
    CanvasGroup cg;

    void Awake() => cg = GetComponent<CanvasGroup>();

    public void SetImmediate(float a) { cg.alpha = a; }

    public IEnumerator FadeTo(float target, float time)
    {
        float start = cg.alpha;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        cg.alpha = target;
    }
}
