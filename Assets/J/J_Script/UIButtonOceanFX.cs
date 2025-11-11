using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIButtonOceanFX : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale / Tilt")]
    public float hoverScale = 1.03f;
    public float pressedScale = 0.95f;
    public float releaseOvershoot = 1.06f;
    public float animTime = 0.10f;
    public float tiltZOnHover = 2f;

    [Header("Ripple (masked by RectMask2D)")]
    public Sprite rippleSprite;                 // 흰→투명 원형 그라디언트
    public Color rippleColor = new Color(1, 1, 1, 0.35f);
    public float rippleStartScale = 0.2f;
    public float rippleEndScale = 1.6f;
    public float rippleTime = 0.35f;

    [Header("Shine Sweep (Hover)")]
    public Image shineImage;                    // 자식 Image (ui_shine_strip)
    public float shineDuration = 0.35f;
    [Range(0f, 1f)] public float shineOpacity = 0.35f;
    public float shinePause = 1.2f;
    public bool shineLoopWhileHover = true;

    [Header("SFX (optional)")]
    public AudioSource audioSource;
    public AudioClip hoverClip;
    public AudioClip pressClip;

    RectTransform rt;
    Vector3 baseScale;
    Quaternion baseRot;
    bool pointerInside;

    // 개별 코루틴 핸들(트윈/샤인 분리)
    Coroutine tweenCo;
    Coroutine shineCo;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseScale = rt.localScale;
        baseRot = rt.localRotation;

        if (shineImage != null)
        {
            shineImage.raycastTarget = false;
            var c = shineImage.color; c.a = 0f; shineImage.color = c; // 최초엔 숨김
        }
    }

    void OnDisable()
    {
        StopTween();
        if (shineCo != null) { StopCoroutine(shineCo); shineCo = null; }
        rt.localScale = baseScale;
        rt.localRotation = baseRot;
        if (shineImage) { var c = shineImage.color; c.a = 0f; shineImage.color = c; }
        pointerInside = false;
    }

    // ---------- Pointer ----------
    public void OnPointerEnter(PointerEventData e)
    {
        pointerInside = true;
        if (hoverClip && audioSource) audioSource.PlayOneShot(hoverClip, 0.6f);

        StopTween(); // ★ 샤인은 건드리지 않음
        tweenCo = StartCoroutine(TweenScaleRot(baseScale * hoverScale, Quaternion.Euler(0, 0, tiltZOnHover), animTime));

        if (shineImage)
        {
            if (shineCo != null) StopCoroutine(shineCo);
            shineCo = StartCoroutine(ShineLoop());
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        pointerInside = false;

        StopTween();
        tweenCo = StartCoroutine(TweenScaleRot(baseScale, baseRot, animTime * 0.8f));

        if (shineCo != null) { StopCoroutine(shineCo); shineCo = null; }
        if (shineImage) { var c = shineImage.color; c.a = 0f; shineImage.color = c; }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (pressClip && audioSource) audioSource.PlayOneShot(pressClip, 0.7f);

        StopTween(); // ★ 샤인 유지
        tweenCo = StartCoroutine(TweenScaleRot(baseScale * pressedScale, Quaternion.identity, animTime * 0.7f));
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (pointerInside)
        {
            StopTween(); // ★ 샤인 유지
            tweenCo = StartCoroutine(BounceThenReturn());
            SpawnRipple(e);
        }
        else
        {
            StopTween();
            tweenCo = StartCoroutine(TweenScaleRot(baseScale, baseRot, animTime * 0.8f));
        }
    }

    void StopTween()
    {
        if (tweenCo != null) { StopCoroutine(tweenCo); tweenCo = null; }
    }

    // ---------- Anim Helpers ----------
    IEnumerator BounceThenReturn()
    {
        yield return TweenScaleRot(baseScale * releaseOvershoot, baseRot, animTime * 0.55f, easeOut: true);
        yield return TweenScaleRot(baseScale * hoverScale, Quaternion.Euler(0, 0, tiltZOnHover), animTime * 0.45f);
    }

    IEnumerator TweenScaleRot(Vector3 targetScale, Quaternion targetRot, float t, bool easeOut = false)
    {
        Vector3 s0 = rt.localScale;
        Quaternion r0 = rt.localRotation;
        float el = 0f;
        while (el < t)
        {
            el += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(el / t);
            float k = easeOut ? EaseOutBack(p) : Smooth(p);
            rt.localScale = Vector3.LerpUnclamped(s0, targetScale, k);
            rt.localRotation = Quaternion.SlerpUnclamped(r0, targetRot, k);
            yield return null;
        }
        rt.localScale = targetScale;
        rt.localRotation = targetRot;
    }

    float Smooth(float x) => x * x * (3f - 2f * x);
    float EaseOutBack(float x) { float c1 = 1.70158f, c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2); }

    // ---------- Ripple ----------
    void SpawnRipple(PointerEventData e)
    {
        if (!rippleSprite) return;

        var go = new GameObject("Ripple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(rt, false);
        var img = go.GetComponent<Image>();
        img.sprite = rippleSprite;
        img.raycastTarget = false;
        img.color = rippleColor; // ★ 머티리얼 사용 안 함

        var rrt = go.GetComponent<RectTransform>();
        rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.5f);

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out local);
        rrt.anchoredPosition = local;

        float baseSize = Mathf.Max(rt.rect.width, rt.rect.height);
        rrt.sizeDelta = new Vector2(baseSize, baseSize);
        rrt.localScale = Vector3.one * rippleStartScale;

        StartCoroutine(RippleAnim(rrt, img));
    }

    IEnumerator RippleAnim(RectTransform r, Image img)
    {
        float t = 0f;
        float start = rippleStartScale;
        float end = rippleEndScale;
        Color c0 = img.color;

        while (t < rippleTime)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / rippleTime);
            float k = Smooth(p);
            float a = Mathf.Lerp(c0.a, 0f, k);
            float s = Mathf.Lerp(start, end, k);
            r.localScale = Vector3.one * s;
            img.color = new Color(c0.r, c0.g, c0.b, a);
            yield return null;
        }
        Destroy(r.gameObject);
    }

    // ---------- Shine ----------
    IEnumerator ShineLoop()
    {
        do
        {
            yield return ShineOnce();
            if (!shineLoopWhileHover) break;
            yield return new WaitForSecondsRealtime(shinePause);
        } while (pointerInside);
    }

    IEnumerator ShineOnce()
    {
        if (!shineImage) yield break;

        var srt = shineImage.rectTransform;
        var btn = rt;

        float halfW = btn.rect.width * 0.55f;   // 시작/종료 지점(버튼 밖)
        Vector2 start = new Vector2(-halfW, 0f);
        Vector2 end = new Vector2(+halfW, 0f);

        srt.anchoredPosition = start;
        var c = shineImage.color; c.a = 0f; shineImage.color = c;

        float t = 0f;
        while (t < shineDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / shineDuration);
            float a = Mathf.Sin(p * Mathf.PI); // 중앙에서 최대
            srt.anchoredPosition = Vector2.Lerp(start, end, p);
            c.a = a * shineOpacity;
            shineImage.color = c;
            yield return null;
        }
        c.a = 0f; shineImage.color = c;
    }
}
