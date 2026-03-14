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

    // ★変更：初期値を Goods（物品）に変更
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

        List<ItemData> displayItems = inventoryList.Where(item => item.category == currentDisplayCategory).ToList();

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