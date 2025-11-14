using UnityEngine;

public class TitleCursorManager : MonoBehaviour
{
    [Tooltip("얼마나 움직여야 '움직였다'고 볼지(픽셀 단위)")]
    public float moveThreshold = 2f;

    private Vector3 lastMousePosition;
    private bool cursorShown = false;

    void Start()
    {
        // 처음엔 커서 숨기기
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined; // 창 밖으로 안 나가게 (원하면 None 로 바꿔도 됨)

        lastMousePosition = Input.mousePosition;
    }

    void Update()
    {
        if (cursorShown)
            return; // 한 번 보여준 후엔 더 체크 안 함

        Vector3 currentPos = Input.mousePosition;
        float distSqr = (currentPos - lastMousePosition).sqrMagnitude;

        // 마우스가 일정 거리 이상 움직였으면 커서 보이게
        if (distSqr > moveThreshold * moveThreshold)
        {
            Cursor.visible = true;
            cursorShown = true;
        }

        lastMousePosition = currentPos;
    }
}
