using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialProximityTrigger : MonoBehaviour
{
    public HaenyeoUIManager ui;
    public int triggerStep = 4;     // "바다에 뛰어 드세요" 단계 인덱스
    public string playerTag = "Player";

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && ui != null)
            ui.AdvanceIfStep(triggerStep);
    }
}
