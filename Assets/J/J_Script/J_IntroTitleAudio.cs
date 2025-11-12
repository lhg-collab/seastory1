using UnityEngine;
using System.Collections;

public class J_IntroTitleAudio : MonoBehaviour
{
    [Header("===== INTRO PHASE =====")]
    [Tooltip("로고 효과음 (SFX 이름)")]
    public string logoSoundName = "LogoSound";

    [Tooltip("인트로 배경음악 (BGM 이름)")]
    public string introBgSoundName = "IntroBG";
    public float introBgFadeInTime = 1.0f;
    [Range(0f, 1f)] public float introBgVolume = 0.30f;

    [Header("===== TITLE PHASE =====")]
    [Tooltip("바다 소리 (Ambient 이름)")]
    public string oceanSoundName = "Ocean";
    public float oceanFadeInTime = 1.5f;
    [Range(0f, 1f)] public float oceanVolume = 0.45f;

    [Tooltip("타이틀 배경음악 (BGM 이름)")]
    public string titleBgmSoundName = "TitleBGM";
    public float titleBgmFadeInTime = 0.5f; 
    [Range(0f, 1f)] public float titleBgmVolume = 0.50f;

    [Header("===== BGM 시작 타이밍 =====")]
    [Tooltip("타이틀로 전환 후 BGM 시작까지 딜레이 (초)")]
    public float bgmStartDelay = 0.1f;  // ← 새로 추가! (기본값 0.5초)

    [Header("===== SEAGULL (랜덤 원샷) =====")]
    [Tooltip("갈매기 소리 (Ambient/SFX 이름)")]
    public string seagullSoundName = "Seagull";
    public Vector2 seagullInterval = new Vector2(6f, 12f);
    public Vector2 seagullVolumeRange = new Vector2(0.15f, 0.25f);

    [Header("===== Boot (타임라인 없이도 시작) =====")]
    public bool bootAtSceneStart = true;
    public bool bootWithLogo = false;

    Coroutine seagullCo;

    void Start()
    {
        if (J_AudioManager.Instance == null)
        {
            Debug.LogError("J_AudioManager not found in scene!");
            return;
        }

        if (bootAtSceneStart)
        {
            OnTitleBackgroundAppear();
            if (bootWithLogo) OnLogoAppear();
        }
    }

    // ===== 타임라인 시그널에서 호출 =====

    public void OnLogoAppear()
    {
        if (J_AudioManager.Instance == null) return;
        J_AudioManager.Instance.PlayOneShotAbs(logoSoundName, 1f);
    }

    public void OnTitleBackgroundAppear()
    {
        if (J_AudioManager.Instance == null) return;
        J_AudioManager.Instance.FadeIn(introBgSoundName, introBgFadeInTime, introBgVolume);
    }

    public void OnIntroEnd()
    {
        StartCoroutine(CoTransitionToTitle());
    }

    IEnumerator CoTransitionToTitle()
    {
        if (J_AudioManager.Instance != null)
            J_AudioManager.Instance.FadeOut(introBgSoundName, 1f, true);

        yield return new WaitForSecondsRealtime(0.5f);
        StartTitlePhase();
    }

    // ===== 타이틀 진입 시 일괄 시작 =====
    void StartTitlePhase()
    {
        if (J_AudioManager.Instance == null) return;

        // 1) 바다 소리
        J_AudioManager.Instance.FadeIn(oceanSoundName, oceanFadeInTime, oceanVolume);

        // 2) 타이틀 BGM (설정 가능한 딜레이 후 페이드인)
        StartCoroutine(CoDelayedBgm());

        // 3) 갈매기 랜덤 원샷 시작
        StartCoroutine(CoDelayedSeagull());
    }

    IEnumerator CoDelayedBgm()
    {
        yield return new WaitForSecondsRealtime(bgmStartDelay);  // ← 1f → bgmStartDelay로 변경!
        if (J_AudioManager.Instance != null)
            J_AudioManager.Instance.FadeIn(titleBgmSoundName, titleBgmFadeInTime, titleBgmVolume);
    }

    IEnumerator CoDelayedSeagull()
    {
        yield return new WaitForSecondsRealtime(2f);
        if (seagullCo != null) StopCoroutine(seagullCo);
        seagullCo = StartCoroutine(CoSeagullLoop());
    }

    IEnumerator CoSeagullLoop()
    {
        while (true)
        {
            float wait = Random.Range(seagullInterval.x, seagullInterval.y);
            yield return new WaitForSecondsRealtime(wait);

            if (J_AudioManager.Instance != null)
            {
                float v = Random.Range(seagullVolumeRange.x, seagullVolumeRange.y);
                J_AudioManager.Instance.PlayOneShotAbs(seagullSoundName, v);
            }
        }
    }

    // ===== 수동 제어 =====

    public void SkipToTitle()
    {
        if (J_AudioManager.Instance != null)
            J_AudioManager.Instance.FadeOut(introBgSoundName, 0.3f, true);

        StartTitlePhase();
    }

    public void FadeOutAll(float duration = 0.8f)
    {
        if (seagullCo != null) { StopCoroutine(seagullCo); seagullCo = null; }

        if (J_AudioManager.Instance != null)
        {
            J_AudioManager.Instance.FadeOut(oceanSoundName, duration, true);
            J_AudioManager.Instance.FadeOut(titleBgmSoundName, duration, true);
            J_AudioManager.Instance.FadeOut(introBgSoundName, duration, true);
        }
    }

    void OnDestroy()
    {
        if (seagullCo != null) StopCoroutine(seagullCo);
    }
}