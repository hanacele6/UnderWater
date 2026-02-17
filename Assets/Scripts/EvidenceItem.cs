using UnityEngine;

public class EvidenceItem : MonoBehaviour, IInteractable 
{
    public ItemData itemData;
    
    // Inspectorから好きなテキストを設定できるようにする（初期値は「調べる」）
    public string promptMessage = "調べる"; 

    public void Interact()
    {
        if (itemData == null) return;
        
        UIManager.Instance.ShowMessage("【" + itemData.itemName + "】 を手に入れた");
        InventoryManager.Instance.AddItem(itemData);
        Destroy(gameObject);
        
        // ★消える時はプロンプトも消す（引数を false から空文字に変更する準備）
        UIManager.Instance.ShowInteractPrompt(""); 
    }

    // ★ IInteractable のルールに従って、プロンプトのテキストを返すメソッドを追加
    public string GetInteractPrompt()
    {
        return promptMessage;
    }
}