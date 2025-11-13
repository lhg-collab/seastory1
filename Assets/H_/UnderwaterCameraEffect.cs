using UnityEngine;

[DisallowMultipleComponent]
public class UnderwaterCameraEffect : MonoBehaviour
{
    [Header("물 높이 / 참조")]
    [Tooltip("물 표면 오브젝트(Plane, Water 등). 있으면 이 오브젝트의 Y를 기준으로 판단")]
    public Transform waterSurface;   // "Water" 오브젝트 넣어도 됨
    public float waterHeight = 0f;   // waterSurface가 없을 때 수동 높이

    [Header("수중 포그 설정")]
    public Color underwaterFogColor = new Color(0.0f, 0.3f, 0.4f, 1f);
    public float underwaterFogDensity = 0.06f;

    [Header("수중 Skybox (선택)")]
    public Material underwaterSkybox;

    // 원래 세팅 저장용
    Color _defaultFogColor;
    float _defaultFogDensity;
    bool _defaultFogEnabled;
    Material _defaultSkybox;

    bool _isUnderwater;

    void Start()
    {
        // 물 오브젝트가 있으면 Y 높이 사용
        if (waterSurface)
            waterHeight = waterSurface.position.y;

        // 현재 포그 / 스카이박스 값 저장
        _defaultFogEnabled = RenderSettings.fog;
        _defaultFogColor = RenderSettings.fogColor;
        _defaultFogDensity = RenderSettings.fogDensity;
        _defaultSkybox = RenderSettings.skybox;
    }

    void Update()
    {
        // 물 높이 갱신(물 오브젝트가 움직이는 타입이면)
        if (waterSurface)
            waterHeight = waterSurface.position.y;

        bool shouldBeUnderwater = transform.position.y < waterHeight;

        if (shouldBeUnderwater != _isUnderwater)
        {
            _isUnderwater = shouldBeUnderwater;
            if (_isUnderwater)
                EnableUnderwater();
            else
                DisableUnderwater();
        }
    }

    void EnableUnderwater()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = underwaterFogColor;
        RenderSettings.fogDensity = underwaterFogDensity;

        if (underwaterSkybox)
            RenderSettings.skybox = underwaterSkybox;

        Debug.Log("[UnderwaterCameraEffect] 수중 효과 ON");
    }

    void DisableUnderwater()
    {
        RenderSettings.fog = _defaultFogEnabled;
        RenderSettings.fogColor = _defaultFogColor;
        RenderSettings.fogDensity = _defaultFogDensity;
        RenderSettings.skybox = _defaultSkybox;

        Debug.Log("[UnderwaterCameraEffect] 수중 효과 OFF");
    }
}
