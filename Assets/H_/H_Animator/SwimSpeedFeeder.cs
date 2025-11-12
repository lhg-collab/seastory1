using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SwimSpeedFeeder : MonoBehaviour
{
    public float smoothing = 10f; // 값 튐 방지
    CharacterController cc; Rigidbody rb; Animator anim;
    float smoothed; int hash;

    void Awake()
    {
        anim = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        hash = Animator.StringToHash("SwimSpeed");
    }

    void Update()
    {
        Vector3 v = Vector3.zero;
        if (cc) v = cc.velocity; else if (rb) v = rb.velocity;

        v.y = 0f;                      // 수평 속도만
        float target = v.magnitude;    // m/s
        smoothed = Mathf.Lerp(smoothed, target, Time.deltaTime * smoothing);
        anim.SetFloat(hash, smoothed);
    }
}
