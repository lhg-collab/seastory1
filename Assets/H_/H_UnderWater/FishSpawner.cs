using System.Collections;
using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    [Header("Area (Water Volume)")]
    public BoxCollider spawnVolume;         // WaterVolume 트리거(박스) 넣기

    [Header("Prefabs")]
    public GameObject[] fishPrefabs;        // 물고기 프리팹(각각 FishSwim 포함)
    public Vector2 scaleRange = new Vector2(0.9f, 1.2f);

    [Header("Spawn Rules")]
    public int initialSpawn = 40;
    public int maxAlive = 120;
    public Vector2 spawnInterval = new Vector2(0.4f, 1.2f); // [min,max] 초
    public Vector2Int schoolSize = new Vector2Int(3, 8);    // 한 번에 뭉치기로
    public float schoolRadius = 2.5f;

    int aliveCount;

    void Start()
    {
        if (!spawnVolume)
            spawnVolume = GetComponent<BoxCollider>();

        // 최초 스폰
        for (int i = 0; i < initialSpawn; i++)
            SpawnOne(Random.insideUnitSphere * schoolRadius);

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (aliveCount < maxAlive)
            {
                // 무리 스폰
                int cnt = Random.Range(schoolSize.x, schoolSize.y + 1);
                Vector3 center = RandomPointInBounds(spawnVolume.bounds);
                for (int i = 0; i < cnt && aliveCount < maxAlive; i++)
                {
                    Vector3 offset = Random.insideUnitSphere * schoolRadius;
                    SpawnOne(offset, center);
                }
            }
            yield return new WaitForSeconds(Random.Range(spawnInterval.x, spawnInterval.y));
        }
    }

    void SpawnOne(Vector3 offset, Vector3? forceCenter = null)
    {
        if (fishPrefabs == null || fishPrefabs.Length == 0 || !spawnVolume) return;

        GameObject prefab = fishPrefabs[Random.Range(0, fishPrefabs.Length)];
        Vector3 pos = forceCenter.HasValue ? forceCenter.Value + offset : RandomPointInBounds(spawnVolume.bounds);
        Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        var go = Instantiate(prefab, pos, rot, transform);
        float s = Random.Range(scaleRange.x, scaleRange.y);
        go.transform.localScale *= s;

        var swim = go.GetComponent<FishSwim>();
        if (swim) swim.swimBounds = spawnVolume; // 수영 영역 주입

        aliveCount++;
    }

    static Vector3 RandomPointInBounds(Bounds b)
    {
        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            Random.Range(b.min.z, b.max.z)
        );
    }
}
