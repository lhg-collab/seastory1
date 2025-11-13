using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIImageFlap : MonoBehaviour
{
    public Sprite[] frames;      // 8프레임 드롭
    public float fps = 12f;
    public bool randomizeStart = true;

    Image img; float t;
    void Awake()
    {
        img = GetComponent<Image>();
        img.preserveAspect = true; img.raycastTarget = false;
        if (randomizeStart && frames.Length > 0) t = Random.value * frames.Length / fps;
    }
    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        t += Time.deltaTime * fps;
        img.sprite = frames[(int)t % frames.Length];
    }
}
