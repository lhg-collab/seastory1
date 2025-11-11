using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    public string itemName = "전복";  // 이 오브젝트가 어떤 해산물인지 (전복, 해삼 등)

    public void Collect()
    {
        // InventoryManager 싱글톤 접근 (인벤토리에 추가하려고)
        InventoryManager inv = InventoryManager.instance;

        if (inv != null)
        {
            // 문자열(예: "전복") → ItemType으로 변환 (AddItem은 ItemType이 필요함)
            ItemType type = StringToItemType(itemName);

            // 인벤토리에 아이템 추가 (성공하면 true 반환)
            bool success = inv.AddItem(type, 1);

            if (success)
            {
                Debug.Log($"{itemName} 채집 완료");

                // 사운드 재생
                if (AudioManager.instance != null)
                    AudioManager.instance.PlayItemGet();

                // 튜토리얼 UI에 알림 (예: “전복을 획득했습니다!”)
                HaenyeoUIManager uIManager = FindObjectOfType<HaenyeoUIManager>();
                if (uIManager != null)
                    uIManager.OnItemCollected();

                // 아이템 오브젝트 파괴 (채집 완료 후 사라짐)
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"인벤토리가 가득 참! {itemName} 채집 실패");
            }
        }
        else
        {
            Debug.LogError("InventoryManager를 찾을 수 없습니다!");
        }
    }

    // 문자열을 ItemType으로 바꾸는 함수
    private ItemType StringToItemType(string name)
    {
        switch (name)
        {
            case "전복": return ItemType.Abalone;
            case "소라": return ItemType.Snail;
            case "해삼": return ItemType.SeaCucumber;
            case "문어": return ItemType.Octopus;
            default:
                Debug.LogWarning($"알 수 없는 아이템 이름: {name}");
                return ItemType.Abalone; // 기본값
        }
    }
}
