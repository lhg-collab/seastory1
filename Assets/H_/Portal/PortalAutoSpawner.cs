using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalAutoSpawner : MonoBehaviour
{
    [Header("어떤 씬에서 생성할지")]
    public string[] spawnOnScenes = { "H_UnderWater" }; // 물속 씬 이름들

    [Header("생성할 포탈 프리팹")]
    public GameObject portalPrefab;

    [Header("생성 위치 오프셋")]
    public float forwardDistance = 2.0f;   // 플레이어 앞 거리
    public float yOffset = 0.0f;           // 살짝 띄우고 싶으면 설정
    public LayerMask groundMask;           // 바닥 맞추기용 (비우면 모든 레이어)

    [Header("방향/정렬 옵션")]
    public bool faceSameDirectionAsPlayer = true; // 플레이어가 보는 방향으로
    public bool alignToSurfaceNormal = false;     // 바닥 법선에 맞추기

    static GameObject lastSpawned; // 중복 방지용(같은 씬에서 하나만)

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 지정한 씬에서만 작동
        if (!IsTargetScene(scene.name)) return;
        SpawnPortalInFront();
    }

    bool IsTargetScene(string sceneName)
    {
        foreach (var n in spawnOnScenes)
            if (!string.IsNullOrEmpty(n) &&
                string.Equals(n, sceneName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    void SpawnPortalInFront()
    {
        if (!portalPrefab)
        {
            Debug.LogError("[PortalAutoSpawner] portalPrefab 미할당!");
            return;
        }

        Transform player = transform; // 이 스크립트를 플레이어에 붙였다고 가정
        Vector3 origin = player.position + Vector3.up * 0.2f;
        Vector3 pos = origin + player.forward * forwardDistance + Vector3.up * yOffset;

        // 바닥으로 Raycast해서 살짝 위에 두기(선택)
        var mask = groundMask.value == 0 ? ~0 : groundMask.value;
        if (Physics.Raycast(origin + Vector3.up * 2f, Vector3.down, out var hit, 10f, mask))
        {
            pos = hit.point + Vector3.up * 0.01f; // 살짝 띄워서 z-fighting 방지
        }

        Quaternion rot = faceSameDirectionAsPlayer
            ? Quaternion.LookRotation(player.forward)
            : Quaternion.identity;

        if (alignToSurfaceNormal && Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var hit2, 5f, mask))
        {
            // 위쪽을 히트 법선에 맞춘 뒤, 전방은 플레이어 전방으로
            var toUp = Quaternion.FromToRotation(Vector3.up, hit2.normal);
            rot = toUp * rot;
        }

        // 같은 씬에 이미 생성돼 있으면 제거(중복 방지)
        if (lastSpawned && lastSpawned.scene == gameObject.scene)
            Destroy(lastSpawned);

        // 근처에 "Portal" 태그가 있으면 또 만들지 않기
        foreach (var p in GameObject.FindGameObjectsWithTag("Portal"))
            if (Vector3.Distance(p.transform.position, pos) < 0.5f) { lastSpawned = p; return; }

        lastSpawned = Instantiate(portalPrefab, pos, rot);
        Debug.Log($"[PortalAutoSpawner] Spawned at {pos}");
    }
}
