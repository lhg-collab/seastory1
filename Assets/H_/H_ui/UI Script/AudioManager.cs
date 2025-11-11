using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("=== 배경음악 ===")]
    public AudioClip titleBGM;           // 타이틀 화면
    public AudioClip tutorialBGM;        // 튜토리얼 화면
    public AudioClip underwaterBGM;      // 물 속

    [Header("=== 효과음 ===")]
    public AudioClip waterEnterSound;    // 물 들어갈 때
    public AudioClip collectStartSound;  // 채집 시작
    public AudioClip collectCompleteSound; // 채집 완료
    public AudioClip buttonClickSound;   // 버튼 클릭
    public AudioClip itemGetSound;       // 아이템 획득
    public AudioClip coinSound; // 상점 판매 시 돈 소리
    public AudioClip outOfStockSound; // 재고 없음
    public AudioClip shopSound; // 상점 여닫는 소리

    [Header("=== 볼륨 설정 ===")]
    [Range(0f, 1f)] public float bgmVolume = 0.3f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    [Header("=== 페이드 설정 ===")]
    public float fadeTime = 1f;  // 음악 전환 시간

    // 오디오 소스
    AudioSource bgmSource;
    AudioSource sfxSource;
    AudioSource sfxLoopSource;


    // 현재 재생 중인 BGM
    private AudioClip currentBGM;
    void Awake()
    {
        // 싱글톤
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // AudioSource 2개 생성
        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxLoopSource = gameObject.AddComponent <AudioSource>();

        // BGM 설정
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;

        // SFX 설정
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;

        // SFX Loop 설정
        sfxLoopSource.loop = true;
        sfxLoopSource.volume = sfxVolume;
        sfxLoopSource.playOnAwake = false ;
    }

    // Start is called before the first frame update
    void Start()
    {
        // 타이틀 BGM 자동 재생
        PlayBGM(titleBGM);
    }
    // ===== BGM 관리 =====

    // BGM 즉시 변경
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (currentBGM == clip) return; // 같은 곡이면 무시

        currentBGM = clip;
        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();

        Debug.Log($" BGM 재생: {clip.name}");
    }

    // BGM 페이드 전환
    public void PlayBGMWithFade(AudioClip clip)
    {
        if (clip == null) return;
        if (currentBGM == clip) return;

        StartCoroutine(FadeBGM(clip));
    }

    // 페이드 효과
    IEnumerator FadeBGM(AudioClip newClip)
    {
        // 페이드 아웃
        float startVolume = bgmSource.volume;

        for (float t = 0; t < fadeTime / 2; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, 0, t / (fadeTime / 2));
            yield return null;
        }

        // 곡 변경
        bgmSource.Stop();
        currentBGM = newClip;
        bgmSource.clip = newClip;
        bgmSource.Play();

        // 페이드 인
        for (float t = 0; t < fadeTime / 2; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0, bgmVolume, t / (fadeTime / 2));
            yield return null;
        }

        bgmSource.volume = bgmVolume;

        Debug.Log($" BGM 페이드 전환: {newClip.name}");
    }

    // 타이틀 BGM
    public void PlayTitleBGM()
    {
        PlayBGMWithFade(titleBGM);
    }

    // 튜토리얼 BGM
    public void PlayTutorialBGM()
    {
        PlayBGMWithFade(tutorialBGM);
    }

    // 물 속 BGM
    public void PlayUnderwaterBGM()
    {
        PlayBGMWithFade(underwaterBGM);
    }

    // BGM 정지
    public void StopBGM()
    {
        bgmSource.Stop();
        currentBGM = null;
    }

    // 추가: 모든 오디오 정지
    public void StopAllAudio()
    {
        // BGM 정지
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }

        // 효과음 정지
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }

        // 루프 효과음 정지
        StopCollectLoop();

        currentBGM = null;

        Debug.Log("모든 오디오 정지!");
    }

    // ===== 효과음 =====

    // 효과음 재생
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    // 편의 함수들
    public void PlayWaterEnter()
    {
        PlaySFX(waterEnterSound);
        Debug.Log("물 들어가는 소리");
    }

    public void PlayCollectLoop()
    {
        if(collectStartSound != null && sfxLoopSource != null)
        {
            // 이미 같은 소리가 재생 중이면 무시
            if (sfxLoopSource.isPlaying && sfxLoopSource.clip == collectStartSound) return;

            sfxLoopSource.clip = collectStartSound;
            sfxLoopSource.volume = sfxVolume;
            sfxLoopSource.Play();
            Debug.Log("채집 루프 시작");
        }
    }

    public void StopCollectLoop()
    {
        if(sfxLoopSource != null && sfxLoopSource.isPlaying)
        {
            sfxLoopSource.Stop();
            sfxLoopSource.clip = null; // 다음 재생을 위해 clip 초기화
            Debug.Log("채집 루프 정지");
        }
    }

    public void PlayCollectComplete()
    {
        PlaySFX(collectCompleteSound);
    }

    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }

    public void PlayItemGet()
    {
        PlaySFX(itemGetSound);
    }

    public void PlayCoinSound()
    {
         PlaySFX(coinSound);
    }

    // 재고 없음 소리 재생
    public void PlayOutOfStock()
    {
        PlaySFX(outOfStockSound);
    }

    // 상점 여닫는 소리 재생
    public void PlayShopSound()
    {
        PlaySFX(shopSound);
    }

    // ===== 볼륨 조절 =====

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    // ===== 음소거 =====

    public void MuteBGM(bool mute)
    {
        if (bgmSource != null)
        {
            bgmSource.mute = mute;
        }
    }

    public void MuteSFX(bool mute)
    {
        if (sfxSource != null)
        {
            sfxSource.mute = mute;
        }
    }
}
