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
        
        // インベントリへの追加とメッセージ表示
        UIManager.Instance.ShowMessage($"【{itemData.itemName}】 を手に入れた");
        InventoryManager.Instance.AddItem(itemData);
        
        // プロンプトを消して、自身を破棄
        UIManager.Instance.ShowInteractPrompt(""); 
        Destroy(gameObject);
    }

    // ★追加：コンテナから生成された時に中身をセットする用
    public void Initialize(ItemData data)
    {
        itemData = data;
    }
}