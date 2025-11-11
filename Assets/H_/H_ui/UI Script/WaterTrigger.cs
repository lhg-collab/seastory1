using UnityEngine;

[DisallowMultipleComponent]
public class WaterTrigger : MonoBehaviour
{
    [Header("수중 시각 효과")]
    [Tooltip("물속에서 보이는 색상")]
    public Color underwaterColor = new Color(0.05f, 0.3f, 0.5f);

    [Tooltip("물속 안개 농도 (0.05~0.15 추천)")]
    [Range(0.0f, 0.5f)]
    public float fogDensity = 0.08f;

    [Header("추가 효과")]
    [Tooltip("물속 카메라 흔들림 강도")]
    public float cameraShakeAmount = 0.01f;

    [Tooltip("카메라 자식에 있는 기포 파티클 오브젝트 이름")]
    public string bubbleParticleName = "BubbleParticles";

    // 내부 상태
    bool isUnderwater = false;
    Camera mainCamera;
    GameObject bubbleParticles;
    Vector3 originalCameraLocalPos;
    Color originalFogColor;
    float originalFogDensity;
    bool originalFogEnabled;

    void Start()
    {
        mainCamera = Camera.main;
        if (!mainCamera)
        {
            Debug.LogError("[WaterTrigger] 메인 카메라를 찾을 수 없습니다!");
        }
        else
        {
            originalCameraLocalPos = mainCamera.transform.localPosition;

            // 카메라 자식에서 버블 파티클 자동 탐색
            var t = mainCamera.transform.Find(bubbleParticleName);
            if (t)
            {
                bubbleParticles = t.gameObject;
                bubbleParticles.SetActive(false);
                Debug.Log($"[WaterTrigger] BubbleParticles 찾음: {bubbleParticles.name}");
            }
            else
            {
                Debug.LogWarning($"[WaterTrigger] '{bubbleParticleName}' 오브젝트를 카메라 자식에서 못 찾음");
            }
        }

        // Fog 원상값 저장
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogEnabled = RenderSettings.fog;

        if (bubbleParticles) bubbleParticles.SetActive(false);
    }

    void Update()
    {
        // 물속 카메라 흔들림
        if (isUnderwater && mainCamera)
        {
            float shakeX = Mathf.Sin(Time.time * 2f) * cameraShakeAmount;
            float shakeY = Mathf.Sin(Time.time * 1.5f) * cameraShakeAmount * 0.5f;
            float shakeZ = Mathf.Cos(Time.time * 1.8f) * cameraShakeAmount * 0.3f;
            mainCamera.transform.localPosition = originalCameraLocalPos + new Vector3(shakeX, shakeY, shakeZ);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isUnderwater) return;

        isUnderwater = true;

        ApplyUnderwaterEffect();

        // 사운드
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayWaterEnter();
            AudioManager.instance.PlayUnderwaterBGM();
        }

        Debug.Log("[WaterTrigger] 물 속으로 들어감");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !isUnderwater) return;

        isUnderwater = false;

        RemoveUnderwaterEffect();

        if (AudioManager.instance != null)
            AudioManager.instance.PlayTutorialBGM();

        Debug.Log("[WaterTrigger] 물 밖으로 나옴");
    }

    // --- 시각 효과 ---
    void ApplyUnderwaterEffect()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = underwaterColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        if (mainCamera)
            mainCamera.backgroundColor = underwaterColor;

        if (bubbleParticles)
            bubbleParticles.SetActive(true);
    }

    void RemoveUnderwaterEffect()
    {
        RenderSettings.fog = originalFogEnabled;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogDensity = originalFogDensity;

        if (mainCamera)
        {
            mainCamera.backgroundColor = new Color(0.5f, 0.7f, 1f);
            mainCamera.transform.localPosition = originalCameraLocalPos;
        }

        if (bubbleParticles)
            bubbleParticles.SetActive(false);
    }

    void OnDestroy()
    {
        // 씬 종료/파괴 시 안전 복구
        RenderSettings.fog = originalFogEnabled;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogDensity = originalFogDensity;

        if (mainCamera)
            mainCamera.transform.localPosition = originalCameraLocalPos;
    }
}
