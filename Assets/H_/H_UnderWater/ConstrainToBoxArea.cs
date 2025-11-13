using UnityEngine;
using System.Linq;

/// 플레이어를 BoxCollider 내부로 강제로 클램프(탈출 불가)
/// - 회전/스케일된 박스도 정확히 처리
/// - CharacterController는 LateUpdate에서 위치 수정(지터 방지 위해 잠깐 disable/enable)
/// - Rigidbody는 FixedUpdate에서 MovePosition 사용
[DefaultExecutionOrder(1000)]
public class ConstrainToBoxArea : MonoBehaviour
{
    [Header("Area")]
    public BoxCollider area;             // 허용 구역(박스)
    [Tooltip("Inspector에서 area 비워두면 이름이 MovementZone인 BoxCollider를 자동으로 찾습니다.")]
    public string autoFindName = "MovementZone";

    [Header("Clamp Options")]
    [Tooltip("XZ 평면만 제한(기본). 끄면 Y축(높이)도 같이 제한합니다.")]
    public bool clampXZOnly = true;
    [Tooltip("벽에 딱 붙는 걸 방지하는 여유 거리")]
    public float margin = 0.05f;

    [Header("Physics")]
    [Tooltip("Rigidbody가 있으면 FixedUpdate에서 MovePosition으로 처리")]
    public bool useRigidbodyIfPresent = true;

    Rigidbody _rb;
    CharacterController _cc;

    void Awake()
    {
        // 자동 영역 찾기
        if (!area)
        {
            area = FindObjectsOfType<BoxCollider>(true)
                    .FirstOrDefault(c => c && c.name == autoFindName);
        }

        // 플레이어 쪽 컴포넌트
        _rb = GetComponent<Rigidbody>();
        _cc = GetComponent<CharacterController>();
    }

    void LateUpdate()
    {
        // Rigidbody가 있으면 물리 루프에서 처리
        if (useRigidbodyIfPresent && _rb) return;
        if (!area) return;

        Vector3 p = transform.position;
        Vector3 clamped = ClampToBox(area, p, clampXZOnly, margin);

        if (p != clamped)
        {
            if (_cc)
            {
                // CC가 있을 때 직접 위치 세팅시 한 프레임 disable/enable이 가장 안정적
                _cc.enabled = false;
                transform.position = clamped;
                _cc.enabled = true;
            }
            else
            {
                transform.position = clamped;
            }
        }
    }

    void FixedUpdate()
    {
        if (!useRigidbodyIfPresent || !_rb || !area) return;

        Vector3 p = _rb.position;
        Vector3 clamped = ClampToBox(area, p, clampXZOnly, margin);

        if (p != clamped)
        {
            // 물리적으로 위치 보정 + 탈출 속도 제거
            _rb.MovePosition(clamped);
            _rb.velocity = Vector3.zero;
        }
    }

    // BoxCollider 내부로 좌표를 클램프(로컬 공간에서 size/center 기준으로 처리)
    public static Vector3 ClampToBox(BoxCollider c, Vector3 worldPos, bool xzOnly, float margin)
    {
        if (!c) return worldPos;

        // 콜라이더 로컬 공간으로 변환
        Transform t = c.transform;
        Vector3 local = t.InverseTransformPoint(worldPos);
        Vector3 half = c.size * 0.5f;
        Vector3 center = c.center;

        // center 기준 좌표로 변환
        local -= center;

        // 각 축 클램프
        local.x = Mathf.Clamp(local.x, -half.x + margin, half.x - margin);
        if (!xzOnly)
            local.y = Mathf.Clamp(local.y, -half.y + margin, half.y - margin);
        local.z = Mathf.Clamp(local.z, -half.z + margin, half.z - margin);

        // 되돌리기
        local += center;
        return t.TransformPoint(local);
    }

    // 에디터에서 영역 확인
    void OnDrawGizmosSelected()
    {
        if (!area) return;
        Gizmos.color = new Color(0, 1, 1, 0.6f);
        Gizmos.matrix = area.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(area.center, area.size);
    }
}
