using System.Collections.Generic; // リスト(List)を使うために必要
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    // 拾ったアイテムデータを保管しておくためのリスト（カバンの中身）
    public List<ItemData> inventoryList = new List<ItemData>();

    [Header("UI References")]
    public Transform contentParent; // リストを並べる場所（後でUIを作ります）
    public GameObject itemButtonPrefab; // リストの1行（ボタン）の設計図

    public TextMeshProUGUI itemDetailText;
    public Image itemDetailIcon;

    private void Awake()
    {
        Instance = this;
    }

    public void AddItem(ItemData newItem)
    {
        inventoryList.Add(newItem);
        UpdateInventoryUI();
    }

    public void UpdateInventoryUI()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemData item in inventoryList)
        {
            GameObject newButton = Instantiate(itemButtonPrefab, contentParent);

            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = item.itemName;
            }

            // 生成したボタンに「クリックされた時の処理（詳細表示）」をプログラムから直接割り当てる
            Button btn = newButton.GetComponent<Button>();
            if (btn != null)
            {
                // ボタンが押されたら、ShowItemDetail(item) を実行するように設定
                btn.onClick.AddListener(() => ShowItemDetail(item)); 
            }
        }
    }

    public void ShowItemDetail(ItemData item)
    {
        if (itemDetailText != null)
        {
            itemDetailText.text = item.description; 
        }

        if (itemDetailIcon != null)
        {
            if (item.itemIcon != null) // アイコン画像が設定されている場合
            {
                itemDetailIcon.sprite = item.itemIcon; // 画像を入れ替える
                itemDetailIcon.enabled = true;         // UIを表示する
            }
            else // アイコン画像が設定されていない場合（透明な四角が出ないようにする）
            {
                itemDetailIcon.enabled = false;        // UIを隠す
            }
        }
    }
    
    public void ClearItemDetail()
    {
        if (itemDetailText != null)
        {
            itemDetailText.text = ""; // テキストを空にする
        }

        if (itemDetailIcon != null)
        {
            itemDetailIcon.enabled = false; // アイコン画像自体を非表示にする
        }
    }
    
}
