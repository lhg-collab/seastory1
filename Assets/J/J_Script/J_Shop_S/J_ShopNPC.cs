using UnityEngine;

[DisallowMultipleComponent]
public class J_ShopNPC : MonoBehaviour
{
    [Tooltip("상점 제목(패널 상단에 표시)")]
    public string shopTitle = "해녀 상점";

    [Tooltip("상호작용 거리")]
    public float interactDistance = 3f;
}
