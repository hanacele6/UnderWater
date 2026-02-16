using UnityEngine;

public class EvidenceItem : MonoBehaviour
{
    public ItemData itemData; 

    public void Interact()
    {
        // 1. もしデータがセットされていなければエラーを出して処理を止める
        if (itemData == null)
        {
            Debug.LogError("このアイテムには ItemData がセットされていません！: " + gameObject.name);
            return;
        }

        // 2. 「〇〇を手に入れた」とメッセージを出す（名前はItemDataから取ってくる）
        UIManager.Instance.ShowMessage("【" + itemData.itemName + "】 を手に入れた");

        // 3. インベントリ（InventoryManager）に、この itemData を渡してリストに追加してもらう
        InventoryManager.Instance.AddItem(itemData);

        // 4. このオブジェクト自体をゲームの世界から消去する
        Destroy(gameObject);
        UIManager.Instance.ShowInteractPrompt(false); // 消える時に「調べる」表記も消す
    }
}