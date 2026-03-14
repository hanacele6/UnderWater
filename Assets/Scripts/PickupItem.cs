using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable 
{
    [Header("アイテム情報")]
    public ItemData itemData;
    public string promptMessage = "拾う"; 

    public string GetInteractPrompt()
    {
        if (itemData != null)
        {
            return $"[{itemData.itemName}] を{promptMessage}";
        }
        return promptMessage;
    }

    public void Interact()
    {
        if (itemData == null) return;
        
        // どちらの場合でもインベントリには必ず追加する
        InventoryManager.Instance.AddItem(itemData);

        // ★変更：カテゴリによって演出を変える
        if (itemData.category == ItemCategory.Material)
        {
            // 素材の場合：画面端にメッセージだけ出して終了
            UIManager.Instance.ShowMessage($"【{itemData.itemName}】 を手に入れた");
        }
        else
        {
            // それ以外（物品、文献、貴重品）の場合：画面中央に詳細画面をドカンと出す
            UIManager.Instance.ShowItemPickupDetail(itemData);
        }
        
        // プロンプトを消して、自身を破棄
        UIManager.Instance.ShowInteractPrompt(""); 
        Destroy(gameObject);
    }

    public void Initialize(ItemData data)
    {
        itemData = data;
    }
}