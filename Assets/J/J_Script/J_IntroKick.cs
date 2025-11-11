using UnityEngine;
using UnityEngine.Playables;

public class J_IntroKick : MonoBehaviour
{
    public PlayableDirector director;
    void Awake()
    {
        Time.timeScale = 1f;      // 혹시 0으로 멈춰있던 경우 해제
        director.time = 0;        // 처음부터
        director.Play();          // 바로 재생
    }
}
