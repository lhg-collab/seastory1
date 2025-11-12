using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class J_AudioManager : MonoBehaviour
{
    public static J_AudioManager Instance { get; private set; }

    [System.Serializable]
    public class Sound
    {
        public string name;           // 호출 키 (Inspector 'Name')
        public AudioClip clip;

        [Range(0f, 1f)] public float volume = 0.7f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop;
        [Range(0f, 1f)] public float spatialBlend = 0f;

        [HideInInspector] public AudioSource source;
    }

    [Header("===== BGM =====")]
    public Sound[] bgmSounds;

    [Header("===== SFX =====")]
    public Sound[] sfxSounds;

    [Header("===== Ambient =====")]
    public Sound[] ambientSounds;

    [Header("Auto names (Inspector Name과 같아야 함)")]
    [SerializeField] string autoBgmName = "IntorBG";
    [SerializeField] string autoLogoSfxName = "LogoSound";
    [SerializeField] string autoSeagullName = "Seagull";

    [Header("Seagull random loop")]
    [SerializeField] Vector2 seagullInterval = new Vector2(6f, 12f);
    [SerializeField] Vector2 seagullVol = new Vector2(0.15f, 0.25f);
    [SerializeField] Vector2 seagullPitch = new Vector2(0.95f, 1.05f);
    [SerializeField] Vector2 seagullPan = new Vector2(-0.35f, 0.35f);

    [Header("Options")]
    public bool dontDestroyOnLoad = true;
    public bool debugLogs = false;

    Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();
    Coroutine seagullCo;

    // ---------------------------------------------------
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        InitializeSounds();
    }

    // ---------------------------------------------------
    void InitializeSounds()
    {
        InitializeSoundArray(bgmSounds, "BGM_");
        InitializeSoundArray(sfxSounds, "SFX_");
        InitializeSoundArray(ambientSounds, "Ambient_");
    }

    void InitializeSoundArray(Sound[] sounds, string prefix)
    {
        if (sounds == null) return;

        foreach (var s in sounds)
        {
            if (s == null || s.clip == null)
            {
                if (debugLogs) Debug.LogWarning($"[Audio] {prefix} null or no clip.");
                continue;
            }

            var go = new GameObject(prefix + s.name);
            go.transform.SetParent(transform, false);

            var src = go.AddComponent<AudioSource>();
            src.clip = s.clip;
            src.volume = 0f;                 // 페이드인/Play 때 올림
            src.pitch = Mathf.Max(0.1f, s.pitch);
            src.loop = s.loop;
            src.playOnAwake = false;
            src.spatialBlend = s.spatialBlend;

            s.source = src;
            soundDictionary[s.name] = s;

            if (debugLogs) Debug.Log($"[Audio] Reg {prefix}{s.name} -> {s.clip.name}");
        }
    }

    // ================== Public Controls ==================

    public void Play(string name)
    {
        if (!soundDictionary.TryGetValue(name, out var s) || s.source == null) { Warn(name); return; }
        s.source.volume = s.volume;       // 실제 출력 볼륨 세팅
        s.source.pitch = Mathf.Max(0.1f, s.pitch);
        s.source.loop = s.loop;
        if (!s.source.isPlaying) s.source.Play();
    }

    public void Stop(string name)
    {
        if (!soundDictionary.TryGetValue(name, out var s) || s.source == null) { Warn(name); return; }
        if (s.source.isPlaying) s.source.Stop();
    }

    public void PlayOneShot(string name, float volumeScale = 1f)
    {
        if (!soundDictionary.TryGetValue(name, out var s) || s.source == null || s.clip == null) { Warn(name); return; }

        float prevVol = s.source.volume;
        float prevPitch = s.source.pitch;

        // 원샷 최종 볼륨 = AudioSource.volume(여기선 1) × scale
        s.source.volume = 1f;
        s.source.pitch = Mathf.Max(0.1f, s.pitch);

        // s.volume 이 0이어도 소리 나게 보호
        float baseVol = (s.volume > 0f) ? s.volume : 1f;
        float scale = Mathf.Clamp01(baseVol * volumeScale);

        s.source.PlayOneShot(s.clip, scale);

        s.source.volume = prevVol;
        s.source.pitch = prevPitch;

        if (debugLogs) Debug.Log($"[Audio] PlayOneShot '{name}' scale={scale}");
    }

    public void PlayOneShotAbs(string name, float absoluteVolume01 = 1f)
    {
        if (!soundDictionary.TryGetValue(name, out var s) || s.source == null || s.clip == null)
        {
            Warn(name);
            return;
        }

        // 기존 방식 대신 PlayClipAtPoint 사용
        AudioSource.PlayClipAtPoint(s.clip, Camera.main.transform.position, Mathf.Clamp01(absoluteVolume01));

        if (debugLogs) Debug.Log($"[Audio] PlayOneShotAbs '{name}' vol={absoluteVolume01} (PlayClipAtPoint)");
    }

    public void FadeIn(string name, float duration, float targetVolume = -1f)
    {
        if (!soundDictionary.TryGetValue(name, out var s) || s.source == null) { Warn(name); return; }
        if (targetVolume < 0f) targetVolume = s.volume;
        StartCoroutine(FadeCoroutine(s, duration, targetVolume, true, false));
    }

    public void FadeOut(string name, float duration, bool stopAfterFade = true)
    {
        if (!soundDictionary.TryGetValue(name, out var s) || s.source == null) { Warn(name); return; }
        StartCoroutine(FadeCoroutine(s, duration, 0f, false, stopAfterFade));
    }

    public bool IsPlaying(string name)
    {
        return soundDictionary.TryGetValue(name, out var s) && s.source != null && s.source.isPlaying;
    }

    public void StopAll()
    {
        foreach (var s in soundDictionary.Values)
            if (s.source != null && s.source.isPlaying) s.source.Stop();
    }

    public void StopAllBGM()
    {
        foreach (var s in bgmSounds)
            if (s != null && s.source != null && s.source.isPlaying) s.source.Stop();
    }

    // ================== Seagull Random Loop ==================

    public void StartSeagulls(string seagullName = "Seagull")
    {
        if (seagullCo != null) StopCoroutine(seagullCo);
        seagullCo = StartCoroutine(CoSeagulls(seagullName));

        // 시작 직후 한 번 보장
        PlayOneShot(seagullName, Random.Range(seagullVol.x, seagullVol.y));
    }

    public void StopSeagulls()
    {
        if (seagullCo != null) { StopCoroutine(seagullCo); seagullCo = null; }
    }

    IEnumerator CoSeagulls(string name)
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(seagullInterval.x, seagullInterval.y));

            if (!soundDictionary.TryGetValue(name, out var s) || s.source == null || s.clip == null) { Warn(name); continue; }

            // 랜덤 피치/팬 + PlayOneShot(볼륨 1 임시 상승 포함)
            float prevPitch = s.source.pitch;
            float prevPan = s.source.panStereo;

            s.source.pitch = Random.Range(seagullPitch.x, seagullPitch.y);
            s.source.panStereo = Random.Range(seagullPan.x, seagullPan.y);

            PlayOneShot(name, Random.Range(seagullVol.x, seagullVol.y));

            s.source.pitch = prevPitch;
            s.source.panStereo = prevPan;
        }
    }

    // ================== Internals ==================

    IEnumerator FadeCoroutine(Sound sound, float duration, float targetVolume, bool playOnStart, bool stopAfterFade)
    {
        if (sound.source == null) yield break;

        float startVolume = sound.source.volume;
        if (playOnStart && !sound.source.isPlaying)
        {
            sound.source.volume = 0f;
            sound.source.pitch = Mathf.Max(0.1f, sound.pitch);
            sound.source.loop = sound.loop;
            sound.source.Play();
            startVolume = 0f;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            sound.source.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(t / duration));
            yield return null;
        }

        sound.source.volume = targetVolume;
        if (stopAfterFade && targetVolume <= 0f) sound.source.Stop();
    }

    void Warn(string name)
    {
        if (debugLogs) Debug.LogWarning($"[Audio] '{name}' not found or has no source/clip.");
    }
}
