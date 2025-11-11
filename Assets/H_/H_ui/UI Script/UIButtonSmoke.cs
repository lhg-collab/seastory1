using UnityEngine;
using UnityEngine.UI;

public class UIButtonSmoke : MonoBehaviour
{
    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => Debug.Log("UI 버튼 클릭 OK"));
        }
        else
        {
            Debug.LogError("이 오브젝트에 Button 컴포넌트가 없습니다.");
        }
    }
}
