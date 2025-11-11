// HarpoonRaycast.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class HarpoonRaycast : MonoBehaviour
{
    [Header("Ray Source")]
    public Transform muzzle;            // 비워두면 cam 중앙에서 레이
    public Camera cam;                  // 비워두면 Camera.main

    [Header("Hit Settings")]
    public float distance = 35f;
    public LayerMask mask = ~0;         // 맞출 레이어
    public bool hitTriggers = true;     // Trigger 콜라이더도 맞기

    [Header("Collector")]
    public Transform collector;         // g.TryCollect에 넘길 주체(보통 플레이어)

    [Header("Debug")]
    public bool drawRay = true;

    void Awake()
    {
        if (!collector) collector = transform;
        if (!cam) cam = Camera.main;
        if (!muzzle && cam) muzzle = cam.transform; // 없으면 카메라 방향 사용
    }

    void Update()
    {
        bool fire =
            (Mouse.current?.leftButton?.wasPressedThisFrame ?? false) ||
            (Keyboard.current?.eKey?.wasPressedThisFrame ?? false);

        if (!fire) return;

        Ray ray;
        if (cam) // 카메라 기준(중앙 조준점)
            ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        else if (muzzle) // 백업: muzzle 기준
            ray = new Ray(muzzle.position, muzzle.forward);
        else
        {
            Debug.LogWarning("[HarpoonRaycast] cam/muzzle가 없습니다.");
            return;
        }

        if (drawRay) Debug.DrawRay(ray.origin, ray.direction * distance, Color.cyan, 0.3f);

        var qti = hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        if (Physics.Raycast(ray, out var hit, distance, mask, qti))
        {
            var g = hit.collider.GetComponentInParent<Gatherable>();
            if (g)
            {
                g.TryCollect(collector);
                Debug.Log($"[HarpoonRaycast] Gatherable hit: {g.name}");
            }
            else
            {
                Debug.Log($"[HarpoonRaycast] Hit {hit.collider.name} (Gatherable 아님)");
            }
        }
        else
        {
            Debug.Log("[HarpoonRaycast] Miss");
        }
    }
}
