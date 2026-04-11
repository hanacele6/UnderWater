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

    [Header("Category Text UI")]
    public TextMeshProUGUI currentCategoryText;

    // カテゴリーの並び順を定義（Q・Eでの切り替え用）
    private readonly ItemCategory[] tabOrder = new ItemCategory[]
    {
        ItemCategory.Material,
        ItemCategory.Sample,
        ItemCategory.Document,
        ItemCategory.Goods,
        ItemCategory.Valuable
    };

    private int currentTabIndex = 0; // 現在選択されているタブのインデックス
    private ItemCategory currentDisplayCategory; 

    [Header("Tab UI Settings")]
    public Button[] tabButtons; 
    public Color activeTabColor = new Color(1f, 1f, 1f, 1f);     
    public Color inactiveTabColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void Awake()
    {
        Instance = this;
        
        // 初期状態のカテゴリーを設定
        currentDisplayCategory = tabOrder[currentTabIndex];
        UpdateTabVisuals();
        UpdateCategoryText();
        ClearItemDetail();
    }

    private void Update()
    {

        if (!contentParent.gameObject.activeInHierarchy) return;
        // Qキーで左のタブへ、Eキーで右のタブへ切り替え
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchTab(-1);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchTab(1);
        }
    }

    // キー入力でタブを切り替える処理
    private void SwitchTab(int direction)
    {
        currentTabIndex += direction;

        // 端まで行ったらループさせる（ループさせたくない場合は制限をかける）
        if (currentTabIndex < 0)
        {
            currentTabIndex = tabOrder.Length - 1;
        }
        else if (currentTabIndex >= tabOrder.Length)
        {
            currentTabIndex = 0;
        }

        ChangeCategory(tabOrder[currentTabIndex]);
    }

    public void ChangeCategory(ItemCategory newCategory)
{
    Debug.Log($"<color=cyan>--- ChangeCategory 呼ばれました！ 変更先: {newCategory} ---</color>");

    currentDisplayCategory = newCategory;
    
    int foundIndex = System.Array.IndexOf(tabOrder, newCategory);
    if (foundIndex != -1)
    {
        currentTabIndex = foundIndex;
    }
    else
    {
        Debug.LogError($"<color=red>エラー：{newCategory} が tabOrder 配列内に見つかりません！</color>");
        currentTabIndex = 0; 
    }

    Debug.Log($"現在のcurrentTabIndex: {currentTabIndex}");

    UpdateTabVisuals(); 
    UpdateCategoryText(); 
    UpdateInventoryUI();
    
    Debug.Log("<color=cyan>--- ChangeCategory 処理完了 ---</color>");
}

    private void UpdateCategoryText()
{
    if (currentCategoryText == null) 
    {
        Debug.LogWarning("CategoryTextがアサインされていません！");
        return;
    }

    string categoryNameJP = GetCategoryNameJP(currentDisplayCategory);
    currentCategoryText.text = categoryNameJP;
    
    Canvas.ForceUpdateCanvases();
    
    Debug.Log($"<color=green>テキストを更新しました: {categoryNameJP}</color>");
}

private string GetCategoryNameJP(ItemCategory category)
{
    switch (category)
    {
        case ItemCategory.Material: return "素材";
        case ItemCategory.Sample:   return "サンプル";
        case ItemCategory.Document: return "文献";
        case ItemCategory.Goods:    return "物品";
        case ItemCategory.Valuable: return "貴重品";
        default: return "不明";
    }
}

private void UpdateTabVisuals()
{
    if (tabButtons == null || tabButtons.Length == 0)
    {
        Debug.LogWarning("TabButtons配列が空です！");
        return;
    }

    for (int i = 0; i < tabButtons.Length; i++)
    {
        if (tabButtons[i] == null) continue;

        // ButtonコンポーネントのColorBlockを使用して色を変更
        var colors = tabButtons[i].colors;
        colors.normalColor = (i == currentTabIndex) ? activeTabColor : inactiveTabColor;
        colors.selectedColor = colors.normalColor; // 選択状態の色も同期
        tabButtons[i].colors = colors;
        
        Image img = tabButtons[i].targetGraphic as Image;
        if(img != null)
        {
             img.color = colors.normalColor;
        }
    }
    Debug.Log("<color=green>タブの色を更新しました。</color>");
}

    public void OnClickMaterialTab() => ChangeCategory(ItemCategory.Material);
    public void OnClickSampleTab() => ChangeCategory(ItemCategory.Sample);
    public void OnClickDocumentTab() => ChangeCategory(ItemCategory.Document);
    public void OnClickGoodsTab() => ChangeCategory(ItemCategory.Goods);
    public void OnClickValuableTab() => ChangeCategory(ItemCategory.Valuable);

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

        ClearItemDetail(); 

        var groupedItems = inventoryList
            .Where(item => item.category == currentDisplayCategory)
            .GroupBy(item => item) 
            .ToList();

        foreach (var group in groupedItems)
        {
            ItemData item = group.Key; 
            int itemCount = group.Count(); 

            GameObject newButton = Instantiate(itemButtonPrefab, contentParent);

            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (itemCount > 1)
                {
                    buttonText.text = $"{item.itemName} <color=#FFFF00>x{itemCount}</color>";
                }
                else
                {
                    buttonText.text = item.itemName;
                }
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

    // 特定のアイテムを何個持っているか数える
    public int GetItemCount(ItemData target)
    {
        return inventoryList.Count(item => item.itemName == target.itemName);
    }

    // 特定のアイテムを指定した数だけ消費（削除）する
    public void RemoveItems(ItemData target, int amount)
    {
        int removedCount = 0;

        // リストを逆から調べると、削除中にズレにくく安全です
        for (int i = inventoryList.Count - 1; i >= 0; i--)
        {
            if (inventoryList[i].itemName == target.itemName)
            {
                inventoryList.RemoveAt(i);
                removedCount++;

                if (removedCount >= amount) break; // 必要な数だけ消したら終了
            }
        }
        
        // 消した後にUIを更新
        UpdateInventoryUI();
    }
}