using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WaterCrossFader : MonoBehaviour
{
    public string waterTag = "Water";
    public float speedUpThreshold = 0.5f;   // 스윔 진입
    public float speedDownThreshold = 0.2f; // 트레딩 복귀
    public float fadeTime = 0.15f;

    Animator anim; CharacterController cc; Rigidbody rb;
    bool inWater, inSwimming;

    // 상태 경로는 Animator 구조에 맞게 정확히!
    static readonly int stTreading = Animator.StringToHash("Base Layer.Water.Treading");
    static readonly int stSwimming = Animator.StringToHash("Base Layer.Water.Swimming");

    void Awake()
    {
        anim = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        anim.applyRootMotion = false;
    }

    void Update()
    {
        if (!inWater) return;

        Vector3 v = cc ? cc.velocity : (rb ? rb.velocity : Vector3.zero);
        v.y = 0f;
        float spd = v.magnitude;

        if (!inSwimming && spd > speedUpThreshold)
        {
            inSwimming = true;
            anim.CrossFade(stSwimming, fadeTime, 0, 0f);
        }
        else if (inSwimming && spd < speedDownThreshold)
        {
            inSwimming = false;
            anim.CrossFade(stTreading, fadeTime, 0, 0f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            inWater = true;
            inSwimming = false;
            anim.CrossFade(stTreading, fadeTime, 0, 0f); // 물 들어가면 트레딩부터
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            inWater = false;
            // 나갈 땐 Ground로 돌아가는 건 Base Layer 전환이 처리
        }
    }
}
