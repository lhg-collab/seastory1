using UnityEngine;
using UnityEngine.Playables;

public class J_IntroKick : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector director;

    [Header("Skip Settings")]
    [Tooltip("스킵 가능 시작 시간 (초)")]
    public float skipAvailableAfter = 1f;

    [Tooltip("스킵 시 이동할 시간 (초, -1이면 타임라인 끝으로)")]
    public float skipToTime = -1f;

    [Header("Input Settings")]
    [Tooltip("마우스 클릭으로 스킵")]
    public bool allowMouseClick = true;

    [Header("Audio")]
    [Tooltip("스킵 시 오디오도 전환")]
    public bool skipAudioToo = true;

    private bool hasSkipped = false;
    private float startTime;

    void Awake()
    {
        Time.timeScale = 1f;      // 혹시 0으로 멈춰있던 경우 해제

        if (director == null)
        {
            director = GetComponent<PlayableDirector>();
        }

        if (director != null)
        {
            director.time = 0;    // 처음부터
            director.Play();      // 바로 재생
        }
        else
        {
            Debug.LogError("[J_IntroKick] PlayableDirector를 찾을 수 없습니다!");
        }
    }

    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        // 이미 스킵했거나, 타임라인이 없으면 무시
        if (hasSkipped || director == null) return;

        // 스킵 가능 시간 이전이면 무시
        if (Time.time - startTime < skipAvailableAfter) return;

        // 타임라인이 끝났으면 무시
        if (director.state != PlayState.Playing) return;

        // 입력 체크
        bool skipRequested = false;

        // 마우스 클릭
        if (allowMouseClick && Input.GetMouseButtonDown(0))
        {
            skipRequested = true;
        }

        if (skipRequested)
        {
            SkipIntro();
        }
    }

    public void SkipIntro()
    {
        if (hasSkipped || director == null) return;

        hasSkipped = true;

        if (skipToTime < 0)
        {
            // 타임라인 끝으로 점프
            director.time = director.duration;
            Debug.Log("[J_IntroKick] 인트로 스킵 → 타임라인 종료");
        }
        else
        {
            // 지정된 시간으로 점프
            director.time = skipToTime;
            Debug.Log($"[J_IntroKick] 인트로 스킵 → {skipToTime}초로 이동");
        }

        // 타임라인 점프 적용
        director.Evaluate();

        // 오디오 스킵
        if (skipAudioToo)
        {
            var audioController = FindObjectOfType<J_IntroTitleAudio>();
            if (audioController != null)
            {
                audioController.SkipToTitle();
                Debug.Log("[J_IntroKick] 오디오도 타이틀로 전환");
            }
        }
    }
}