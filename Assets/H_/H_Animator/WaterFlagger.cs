using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WaterFlagger : MonoBehaviour
{
    public string waterTag = "Water";
    public LayerMask waterLayers; // (선택) 물 레이어가 따로 있으면 지정
    Animator anim; int hash;

    void Awake()
    {
        anim = GetComponent<Animator>();
        hash = Animator.StringToHash("InWater");
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsWater(other)) anim.SetBool(hash, true);
    }
    void OnTriggerExit(Collider other)
    {
        if (IsWater(other)) anim.SetBool(hash, false);
    }

    bool IsWater(Collider c)
    {
        if (!string.IsNullOrEmpty(waterTag) && c.CompareTag(waterTag)) return true;
        if (waterLayers != 0 && ((1 << c.gameObject.layer) & waterLayers) != 0) return true;
        return false;
    }
}
