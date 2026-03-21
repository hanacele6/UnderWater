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
    public TextMeshProUGUI itemDetailNameText; 
    public TextMeshProUGUI itemDetailDescText;
    public Image itemDetailIcon;

 
    private ItemCategory currentDisplayCategory = ItemCategory.Goods; 

    private void Awake()
    {
        Instance = this;

        ClearItemDetail();
    }

    public void AddItem(ItemData newItem)
    {
        inventoryList.Add(newItem);
        UpdateInventoryUI();
    }
    public void ChangeTabToGoods()
    {
        currentDisplayCategory = ItemCategory.Goods;
        UpdateInventoryUI();
    }

    public void ChangeTabToDocument()
    {
        currentDisplayCategory = ItemCategory.Document;
        UpdateInventoryUI();
    }

    public void ChangeTabToMaterial()
    {
        currentDisplayCategory = ItemCategory.Material;
        UpdateInventoryUI();
    }

    public void ChangeTabToValuable()
    {
        currentDisplayCategory = ItemCategory.Valuable;
        UpdateInventoryUI();
    }

    public void UpdateInventoryUI()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        ClearItemDetail(); 

     
        var groupedItems = inventoryList
            .Where(item => item.category == currentDisplayCategory)
            .GroupBy(item => item) // 同じアイテムデータでまとめる
            .ToList();

        foreach (var group in groupedItems)
        {
            ItemData item = group.Key; // アイテムの種類
            int itemCount = group.Count(); // そのアイテムを何個持っているか

            GameObject newButton = Instantiate(itemButtonPrefab, contentParent);

            // ① アイテム名の表示
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = item.itemName;
            }

            // ② 個数の表示（例：「x3」）※プレハブに個数用のテキストを追加した場合
            // Transform countTransform = newButton.transform.Find("CountText");
            // if (countTransform != null)
            // {
            //     TextMeshProUGUI countText = countTransform.GetComponent<TextMeshProUGUI>();
            //     countText.text = itemCount > 1 ? $"x{itemCount}" : ""; // 1個の時は数字を出さない
            // }

            // 名前の横に直接個数を足しちゃう
            if (buttonText != null && itemCount > 1)
            {
                buttonText.text = $"{item.itemName} <color=#FFFF00>x{itemCount}</color>";
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
        if (itemDetailNameText != null) itemDetailNameText.text = item.itemName;
        if (itemDetailDescText != null) itemDetailDescText.text = item.description;

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
        if (itemDetailNameText != null) itemDetailNameText.text = ""; 
        if (itemDetailDescText != null) itemDetailDescText.text = ""; 
        if (itemDetailIcon != null) itemDetailIcon.enabled = false;
    }

    public void RemoveItem(ItemData itemToRemove)
    {
        if (inventoryList.Contains(itemToRemove))
        {
            inventoryList.Remove(itemToRemove);
            UpdateInventoryUI(); 
        }
    }
}