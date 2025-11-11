using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class GatherableSpawner : MonoBehaviour
{
    [Header("범위 (필수)")]
    public BoxCollider area;                 // 스폰 영역(가로/세로는 XZ, 높이는 크게)

    [Header("프리팹들")]
    public GameObject[] gatherablePrefabs;   // Gatherable 포함된 프리팹

    [Header("개수/리스폰")]
    public int initialCount = 40;
    public int maxAlive = 120;
    public Vector2 respawnDelayRange = new Vector2(5f, 12f);

    [Header("배치 규칙(지상용)")]
    public LayerMask placementMask;          // Terrain이 포함된 레이어(필수)
    public bool requireTerrain = true;       // 꼭 Terrain 위에만 스폰할지
    public bool alignToNormal = true;        // 표면 법선 정렬
    public float surfaceOffset = 0.03f;      // 지면에서 살짝 띄우기
    public float maxSlope = 45f;             // 경사 제한(도)
    public float minSpacing = 1.0f;          // 채집물 사이 최소 간격
    public int placementAttempts = 20;       // 한 개 스폰 시 시도 횟수
    public float castUp = 120f;              // 위에서 쏠 높이

    [Header("물고기(Fish) 설정")]
    public LayerMask fishLayer;              // Fish 레이어 마스크 (루트/자식 모두 검사)

    [Header("특정 프리팹 회전")]
    public string specialPrefabName = "GAOHRI 리"; // 이 이름이면 X축 -90 고정
    public float specialXRotation = -90f;

    List<Gatherable> _alive = new List<Gatherable>();

    void Reset() { area = GetComponent<BoxCollider>(); if (area) area.isTrigger = true; }

    void Start()
    {
        if (!area) area = GetComponent<BoxCollider>();
        for (int i = 0; i < initialCount; i++) TrySpawn();
    }

    public void NotifyCollected(Gatherable g)
    {
        if (g) { _alive.Remove(g); Destroy(g.gameObject); }
        StartCoroutine(RespawnLater(Random.Range(respawnDelayRange.x, respawnDelayRange.y)));
    }
    IEnumerator RespawnLater(float t)
    {
        yield return new WaitForSeconds(t);
        if (_alive.Count < maxAlive) TrySpawn();
    }

    bool TrySpawn()
    {
        if (gatherablePrefabs == null || gatherablePrefabs.Length == 0 || !area) return false;

        for (int attempt = 0; attempt < placementAttempts; attempt++)
        {
            // 어떤 프리팹을 놓을지 먼저 결정
            var prefab = gatherablePrefabs[Random.Range(0, gatherablePrefabs.Length)];
            bool isFish = IsPrefabOrAnyChildInLayer(prefab, fishLayer);
            bool isSpecial = IsSpecialPrefab(prefab);

            if (isFish)
            {
                // ✅ Fish: Terrain 체크/정렬 없이 area 박스 내부 아무 위치에 스폰
                Vector3 p = RandomPointInBox(area.bounds);
                if (!IsFarEnough(p, minSpacing)) continue;

                // 기본은 Y 랜덤 회전
                Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                // "GAOHRI 리" 라면 X축 -90, Y는 랜덤 유지
                if (isSpecial) rot = Quaternion.Euler(specialXRotation, Random.Range(0f, 360f), 0);

                var go = Instantiate(prefab, p, rot, transform);

                var g = go.GetComponent<Gatherable>();
                if (!g) g = go.AddComponent<Gatherable>(); // 안전장치
                g.ownerSpawner = this;

                _alive.Add(g);
                return true;
            }
            else
            {
                // 기존 지상 배치 로직
                Vector3 posXZ = RandomPointInBoxXZ(area.bounds);
                Vector3 castFrom = posXZ + Vector3.up * castUp;

                if (Physics.Raycast(castFrom, Vector3.down, out var hit, castUp * 2f, placementMask, QueryTriggerInteraction.Ignore))
                {
                    if (requireTerrain && !hit.collider.GetComponent<Terrain>()) continue;
                    if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope) continue;
                    if (!IsFarEnough(hit.point, minSpacing)) continue;

                    Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                    if (alignToNormal) rot = Quaternion.FromToRotation(Vector3.up, hit.normal) * rot;

                    // 특수 프리팹이면 X축 -90으로 오버라이드 (지상에도 적용 필요하면 유지)
                    if (isSpecial)
                    {
                        // 지상 정렬을 쓰는 경우에도 X축은 강제로 -90, 나머지는 기존 회전 유지
                        Vector3 e = rot.eulerAngles;
                        rot = Quaternion.Euler(specialXRotation, e.y, e.z);
                    }

                    var go = Instantiate(prefab, hit.point + hit.normal * surfaceOffset, rot, transform);

                    var g = go.GetComponent<Gatherable>();
                    if (!g) g = go.AddComponent<Gatherable>(); // 안전장치
                    g.ownerSpawner = this;

                    _alive.Add(g);
                    return true;
                }
            }
        }
        return false;
    }

    // ---------- Helpers ----------

    // 프리팹(루트+자식) 중 하나라도 주어진 레이어마스크에 포함되면 true
    bool IsPrefabOrAnyChildInLayer(GameObject prefab, LayerMask mask)
    {
        int m = mask.value;
        // true => 비활성 포함
        var transforms = prefab.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            int layer = transforms[i].gameObject.layer;
            if ((m & (1 << layer)) != 0) return true;
        }
        return false;
    }

    bool IsSpecialPrefab(GameObject prefab)
    {
        // 이름 완전 일치(대소문자 구분 없음). 필요하면 Contains로 바꿔도 됨.
        return !string.IsNullOrEmpty(specialPrefabName) &&
               prefab.name.Equals(specialPrefabName, System.StringComparison.OrdinalIgnoreCase);
    }

    bool IsFarEnough(Vector3 p, float minDist)
    {
        float sq = minDist * minDist;
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            var g = _alive[i];
            if (!g) { _alive.RemoveAt(i); continue; }
            if ((g.transform.position - p).sqrMagnitude < sq) return false;
        }
        return true;
    }

    static Vector3 RandomPointInBoxXZ(Bounds b)
    {
        return new Vector3(Random.Range(b.min.x, b.max.x), b.max.y, Random.Range(b.min.z, b.max.z));
    }

    // 3D 볼륨 내부 무작위 점
    static Vector3 RandomPointInBox(Bounds b)
    {
        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            Random.Range(b.min.z, b.max.z)
        );
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!area) return;
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.2f);
        Gizmos.DrawCube(area.bounds.center, area.bounds.size);
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.7f);
        Gizmos.DrawWireCube(area.bounds.center, area.bounds.size);
    }
#endif
}
