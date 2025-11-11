// CrosshairDrawer.cs
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CrosshairDrawer : MonoBehaviour
{
    [Header("Visibility")]
    public bool onlyWhenAiming = true;   // 우클릭 조준 중에만 보이게
    public bool alwaysShow = false;      // 테스트용: 항상 표시

    [Header("Shape")]
    public Color color = Color.white;
    public float thickness = 2f;         // 선 굵기(px)
    public float length = 12f;           // 각 팔 길이(px)
    public float gap = 6f;               // 중앙 간격(px)

    [Header("Outline")]
    public bool drawOutline = true;
    public Color outlineColor = new Color(0, 0, 0, 0.85f);
    public float outlineThickness = 1.5f;

    [Header("Scaling")]
    public bool scaleWithResolution = true;
    public float referenceHeight = 1080f;

    [Header("Optional: spread by movement")]
    public Transform speedTarget;        // CharacterController나 Rigidbody가 붙은 대상(보통 플레이어)
    public float moveSpread = 6f;        // 이동 시 추가로 벌어질 최대 px
    public float maxMoveSpeed = 6f;      // 이 속도에서 moveSpread 최대
    public float spreadLerp = 10f;       // 스프레드 보간 속도

    float _curMoveSpread;
    CharacterController _cc;
    Rigidbody _rb;
    Texture2D _tex;

    void Awake()
    {
        _tex = Texture2D.whiteTexture;
        if (!speedTarget) speedTarget = transform;
        _cc = speedTarget ? speedTarget.GetComponent<CharacterController>() : null;
        _rb = speedTarget ? speedTarget.GetComponent<Rigidbody>() : null;
    }

    void Update()
    {
        // 이동 속도 기반 스프레드(선택)
        float speed = 0f;
        if (_cc) speed = _cc.velocity.magnitude;
        else if (_rb) speed = _rb.velocity.magnitude;

        float targetSpread = (maxMoveSpeed > 0f)
            ? Mathf.Clamp01(speed / maxMoveSpeed) * moveSpread
            : 0f;

        _curMoveSpread = Mathf.Lerp(_curMoveSpread, targetSpread, Time.deltaTime * spreadLerp);
    }

    void OnGUI()
    {
        // 레이아웃 이벤트에선 그리지 않음
        if (Event.current.type != EventType.Repaint) return;

        // 표시 조건
        bool aimingNow = Mouse.current?.rightButton?.isPressed ?? false;
        if (!alwaysShow && onlyWhenAiming && !aimingNow) return;

        float k = scaleWithResolution ? (Screen.height / referenceHeight) : 1f;
        float th = Mathf.Max(1f, thickness * k);
        float len = Mathf.Max(2f, length * k);
        float gp = Mathf.Max(0f, gap * k) + _curMoveSpread;

        Vector2 c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // 4개 팔의 사각형 계산
        Rect up = new Rect(c.x - th * 0.5f, c.y - gp - len, th, len);
        Rect down = new Rect(c.x - th * 0.5f, c.y + gp, th, len);
        Rect left = new Rect(c.x - gp - len, c.y - th * 0.5f, len, th);
        Rect right = new Rect(c.x + gp, c.y - th * 0.5f, len, th);

        if (drawOutline)
        {
            float ot = outlineThickness;
            Color oc = outlineColor;
            // 각 팔에 외곽선(조금 큰 사각형) 먼저 그림
            DrawRect(Expand(up, ot), oc);
            DrawRect(Expand(down, ot), oc);
            DrawRect(Expand(left, ot), oc);
            DrawRect(Expand(right, ot), oc);
        }

        // 본체
        DrawRect(up, color);
        DrawRect(down, color);
        DrawRect(left, color);
        DrawRect(right, color);
    }

    void DrawRect(Rect r, Color c)
    {
        Color prev = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, _tex);
        GUI.color = prev;
    }

    Rect Expand(Rect r, float amount)
    {
        return new Rect(r.x - amount, r.y - amount, r.width + amount * 2f, r.height + amount * 2f);
    }
}
