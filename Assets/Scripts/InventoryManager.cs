using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq; 

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public List<ItemData> inventoryList = new List<ItemData>();

    [Header("UI References")]
    public Transform contentParent; 
    public GameObject itemButtonPrefab; 
    public TextMeshProUGUI itemDetailText;
    public Image itemDetailIcon;

    // ==========================================
    // 現在開いているタブ（カテゴリ）を記憶する変数
    // 初期値として「証拠品(Evidence)」を表示するようにしています
    // ==========================================
    private ItemCategory currentDisplayCategory = ItemCategory.Evidence; 

    private void Awake()
    {
        Instance = this;
    }

    public void AddItem(ItemData newItem)
    {
        inventoryList.Add(newItem);
        UpdateInventoryUI();
    }

    // ==========================================
    // UIの「タブボタン」から呼び出すためのメソッド群
    // ==========================================
    public void ChangeTabToEvidence()
    {
        currentDisplayCategory = ItemCategory.Evidence;
        UpdateInventoryUI(); // タブを変えたら画面を更新！
    }

    public void ChangeTabToMaterial()
    {
        currentDisplayCategory = ItemCategory.Material;
        UpdateInventoryUI();
    }

    public void ChangeTabToConsumable()
    {
        currentDisplayCategory = ItemCategory.Consumable;
        UpdateInventoryUI();
    }

    public void UpdateInventoryUI()
    {
        // 1. 今表示されている古いボタンを全部消す
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // タブを切り替えた時は、右側の詳細画面も一旦空っぽにする
        ClearItemDetail(); 

        // ==========================================
        // 全部ではなく「今のカテゴリに合っているもの」だけを LINQ で抽出！
        // ==========================================
        List<ItemData> displayItems = inventoryList.Where(item => item.category == currentDisplayCategory).ToList();

        // 3. 抽出したアイテムだけでボタンを作る
        foreach (ItemData item in displayItems)
        {
            GameObject newButton = Instantiate(itemButtonPrefab, contentParent);

            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = item.itemName;
            }

            Button btn = newButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowItemDetail(item)); 
            }
        }
    }

    public void ShowItemDetail(ItemData item)
    {
        if (itemDetailText != null) itemDetailText.text = item.description; 

        if (itemDetailIcon != null)
        {
            if (item.itemIcon != null) 
            {
                itemDetailIcon.sprite = item.itemIcon; 
                itemDetailIcon.enabled = true;        
            }
            else 
            {
                itemDetailIcon.enabled = false;        
            }
        }
    }
    
    public void ClearItemDetail()
    {
        if (itemDetailText != null) itemDetailText.text = ""; 
        if (itemDetailIcon != null) itemDetailIcon.enabled = false; 
    }
}