using UnityEngine;

public class UIBirdMover : MonoBehaviour
{
    public float speed = 220f;
    public float floatAmp = 20f, floatSpeed = 1f;

    RectTransform rt, canvas;
    Vector2 start;
    bool initialized = false;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    void Update()
    {
        // 첫 프레임에 시작 위치 저장
        if (!initialized)
        {
            start = rt.anchoredPosition;
            initialized = true;
        }

        float x = rt.anchoredPosition.x + speed * Time.deltaTime;
        float y = start.y + Mathf.Sin(Time.time * floatSpeed) * floatAmp;
        float limit = canvas.rect.width * 0.5f + rt.rect.width;

        if (x > limit) x = -limit;
        rt.anchoredPosition = new Vector2(x, y);
    }
}