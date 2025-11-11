using UnityEngine;
using System.Collections;

public class UIBirdSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject birdPrefab;           // UIBirdMover + UIImageFlap 붙은 프리팹
    public RectTransform spawnParent;       // 새가 생성될 부모 (Canvas 하위)
    public float minY = -100f;              // 최소 Y 위치
    public float maxY = 100f;               // 최대 Y 위치
    public float minInterval = 2f;          // 최소 스폰 간격
    public float maxInterval = 5f;          // 최대 스폰 간격
    public int maxBirds = 10;               // 최대 새 개수 (성능 고려)

    [Header("Bird Variation")]
    public float minSpeed = 180f;           // 최소 속도
    public float maxSpeed = 260f;           // 최대 속도
    public float minScale = 0.8f;           // 최소 크기
    public float maxScale = 1.2f;           // 최대 크기

    RectTransform canvasRT;
    int birdCount = 0;

    void Start()
    {
        if (spawnParent == null)
            spawnParent = GetComponent<RectTransform>();

        canvasRT = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float interval = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(interval);

            if (birdCount < maxBirds)
                SpawnBird();
        }
    }

    void SpawnBird()
    {
        if (birdPrefab == null) return;

        GameObject bird = Instantiate(birdPrefab, spawnParent);
        RectTransform birdRT = bird.GetComponent<RectTransform>();

        // 랜덤 Y 위치 (화면 안에서)
        float randomY = Random.Range(minY, maxY);

        // 화면 왼쪽 밖에서 시작
        float startX = -canvasRT.rect.width * 0.5f - birdRT.rect.width;
        birdRT.anchoredPosition = new Vector2(startX, randomY);

        // 속도와 크기 랜덤화
        UIBirdMover mover = bird.GetComponent<UIBirdMover>();
        if (mover != null)
        {
            mover.speed = Random.Range(minSpeed, maxSpeed);
            mover.floatSpeed = Random.Range(0.8f, 1.5f);  // 파동 속도도 다양화
        }

        float scale = Random.Range(minScale, maxScale);
        bird.transform.localScale = Vector3.one * scale;

        birdCount++;

        // 일정 시간 후 자동 삭제 (메모리 관리)
        float lifetime = (canvasRT.rect.width * 2f) / mover.speed + 5f;
        Destroy(bird, lifetime);
        StartCoroutine(DecrementCountAfter(lifetime));
    }

    IEnumerator DecrementCountAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        birdCount--;
    }
}