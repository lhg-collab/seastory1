using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public class GatherAnimationPlayer : MonoBehaviour
{
    [Header("재생할 채집 모션")]
    public AnimationClip gatherClip;      // 예: Pick Fruit
    public int framesToPlay = 60;         // 딱 이 프레임까지만 재생
    public bool applyFootIK = true;       // 필요시

    [Header("옵션")]
    public bool blockReTriggerWhilePlaying = true; // 재생 중 중복 방지
    public bool pauseMovementInput = true;        // 원하면 true로 바꿔 입력만 잠깐 막기(컨트롤러 비활성 X)

    Animator _anim;
    PlayableGraph _graph;
    bool _playing;

    void Awake()
    {
        _anim = GetComponentInParent<Animator>();
        if (!_anim)
            _anim = GetComponent<Animator>();
    }

    void OnDisable()
    {
        StopAndDispose();
    }

    public void PlayFirstFrames()
    {
        if (!gatherClip)
        {
            Debug.LogWarning("[GatherAnimationPlayer] gatherClip이 없습니다.");
            return;
        }
        if (blockReTriggerWhilePlaying && _playing) return;

        float duration = framesToPlay / Mathf.Max(1f, gatherClip.frameRate); // 60 / fps(초)

        // 기존 그래프 정리
        StopAndDispose();

        // Playables로 클립 직재생 (컨트롤러 건드리지 않음)
        _graph = PlayableGraph.Create("GatherSegment");
        var output = AnimationPlayableOutput.Create(_graph, "Anim", _anim);
        var clipPlayable = AnimationClipPlayable.Create(_graph, gatherClip);
        clipPlayable.SetApplyFootIK(applyFootIK);
        clipPlayable.SetTime(0);
        clipPlayable.SetSpeed(1.0);
        output.SetSourcePlayable(clipPlayable);
        _graph.Play();

        _playing = true;
        if (pauseMovementInput) StartCoroutine(TemporarilyPauseLookAndMove(duration));
        StartCoroutine(StopAfter(duration));
    }

    IEnumerator StopAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StopAndDispose();
        _playing = false;
    }

    void StopAndDispose()
    {
        if (_graph.IsValid())
        {
            _graph.Stop();
            _graph.Destroy();
        }
    }

    // (선택) 이동/룩 입력만 잠깐 제한 — 컨트롤러는 끄지 않음(추락 방지)
    IEnumerator TemporarilyPauseLookAndMove(float seconds)
    {
        // StarterAssets가 있으면 룩 입력만 잠깐 끄기
        foreach (var b in Resources.FindObjectsOfTypeAll<Behaviour>())
        {
            if (b == null) continue;
            var full = b.GetType().FullName;
            if (full == "StarterAssets.StarterAssetsInputs")
            {
                TrySetBool(b, "cursorInputForLook", false);
                // 필요하면 move 입력도 0으로: TrySetVector2(b, "move", Vector2.zero);
            }
            if (b.GetType().Name == "CinemachineInputProvider")
                b.enabled = false;
        }

        yield return new WaitForSeconds(seconds);

        foreach (var b in Resources.FindObjectsOfTypeAll<Behaviour>())
        {
            if (b == null) continue;
            var full = b.GetType().FullName;
            if (full == "StarterAssets.StarterAssetsInputs")
            {
                TrySetBool(b, "cursorInputForLook", true);
                // TrySetVector2(b, "move", Vector2.zero);
            }
            if (b.GetType().Name == "CinemachineInputProvider")
                b.enabled = true;
        }
    }

    // 리플렉션 유틸
    static void TrySetBool(object obj, string name, bool v)
    {
        var t = obj.GetType();
        var f = t.GetField(name);
        if (f != null && f.FieldType == typeof(bool)) { f.SetValue(obj, v); return; }
        var p = t.GetProperty(name);
        if (p != null && p.PropertyType == typeof(bool) && p.CanWrite) p.SetValue(obj, v);
    }
    /*
    static void TrySetVector2(object obj, string name, Vector2 v)
    {
        var t = obj.GetType();
        var f = t.GetField(name);
        if (f != null && f.FieldType == typeof(Vector2)) { f.SetValue(obj, v); return; }
        var p = t.GetProperty(name);
        if (p != null && p.PropertyType == typeof(Vector2) && p.CanWrite) p.SetValue(obj, v);
    }
    */
}
