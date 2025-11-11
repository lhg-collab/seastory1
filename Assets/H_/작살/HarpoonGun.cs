using UnityEngine;
using UnityEngine.InputSystem;

public class HarpoonGun : MonoBehaviour
{
    [Header("Refs")]
    public Transform muzzle;                      // 총구
    public LineRenderer rope;                     // 로프 표현 (선택)

    [Header("Projectile")]
    public HarpoonProjectile projectilePrefab;    // 아래 스크립트 붙은 프리팹
    public float launchSpeed = 45f;
    public float maxDistance = 35f;
    public LayerMask hitMask = ~0;

    [Header("Collect")]
    public Transform collector; // 맞은 Gatherable에 TryCollect로 넘길 주체(보통 플레이어)

    HarpoonProjectile _current;

    void Awake()
    {
        if (!collector) collector = transform;
        if (rope)
        {
            rope.positionCount = 2;
            rope.enabled = false;
        }
    }

    void Update()
    {
        // 좌클릭 또는 E로 발사 (원하면 키 바꾸세요)
        bool fire =
            (Mouse.current?.leftButton?.wasPressedThisFrame ?? false) ||
            (Keyboard.current?.eKey?.wasPressedThisFrame ?? false);

        if (fire && _current == null && projectilePrefab && muzzle)
        {
            _current = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
            _current.Init(this, launchSpeed, maxDistance, hitMask, collector);
            if (rope) rope.enabled = true;
        }

        // 로프 갱신
        if (rope)
        {
            if (_current)
            {
                rope.SetPosition(0, muzzle.position);
                rope.SetPosition(1, _current.transform.position);
            }
            else if (rope.enabled)
            {
                rope.enabled = false;
            }
        }
    }

    // 발사체 수명 종료/회수 콜백
    public void OnProjectileDone()
    {
        _current = null;
        if (rope) rope.enabled = false;
    }
}
