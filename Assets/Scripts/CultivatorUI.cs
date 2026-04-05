using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CultivatorUI : MonoBehaviour
{
    [Header("UI参照")]
    public GameObject seedSelectionPanel; // 種リストの背景パネル
    public Transform seedListParent;      // ボタンを並べる親（ScrollViewのContent）
    public GameObject seedButtonPrefab;   // 種1個分のボタンプレハブ

    private PlanterSlot currentTargetSlot; // 今クリックして選択中の土マス

    void Start()
    {
        if (seedSelectionPanel != null) seedSelectionPanel.SetActive(false);
    }

    // 土マスから呼ばれてUIを開く
    public void OpenSeedList(PlanterSlot targetSlot)
    {
        currentTargetSlot = targetSlot;
        seedSelectionPanel.SetActive(true);
        RefreshSeedList();
    }

    // UIを閉じる（キャンセルボタンなど用）
    public void CloseSeedList()
    {
        seedSelectionPanel.SetActive(false);
        currentTargetSlot = null;
    }

    // 持っている種リストを生成する
    private void RefreshSeedList()
    {
        // 古いボタンをお掃除
        foreach (Transform child in seedListParent) Destroy(child.gameObject);

        if (InventoryManager.Instance == null || CultivatorConsole.Instance == null) return;

        // 💡 インベントリにあるアイテムと、全レシピの「必要な種」を照らし合わせる
        List<CultivationRecipeData> availableRecipes = new List<CultivationRecipeData>();
        
        foreach (var recipe in CultivatorConsole.Instance.allCultivationRecipes)
        {
            // インベントリの中に、このレシピに必要な種（細胞）が含まれているか？
            if (InventoryManager.Instance.inventoryList.Contains(recipe.requiredSeed))
            {
                availableRecipes.Add(recipe);
            }
        }

        // もし植えられる種が1つも無い場合
        if (availableRecipes.Count == 0)
        {
            GameObject msgObj = Instantiate(seedButtonPrefab, seedListParent);
            msgObj.GetComponentInChildren<TextMeshProUGUI>().text = "植えられる種（細胞）を持っていません";
            msgObj.GetComponent<Button>().interactable = false;
            return;
        }

        // 植えられる種（レシピ）のボタンを生成
        foreach (var recipe in availableRecipes)
        {
            GameObject btnObj = Instantiate(seedButtonPrefab, seedListParent);
            TextMeshProUGUI textUI = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            
            // 例：「発光細胞 👉 光るキノコ」のように表示
            if (textUI != null) textUI.text = $"{recipe.requiredSeed.itemName} を植える";

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                // ボタンを押した時の処理を登録
                btn.onClick.AddListener(() => OnSeedButtonClicked(recipe));
            }
        }
    }

    // 種ボタンを押した時の処理
    private void OnSeedButtonClicked(CultivationRecipeData selectedRecipe)
    {
        if (currentTargetSlot == null) return;

        // 1. インベントリから種（細胞）を1つ消費する
        InventoryManager.Instance.RemoveItem(selectedRecipe.requiredSeed);

        // 2. 選択中の土に植える！（※GameManagerから現在の日付も渡す）
        currentTargetSlot.PlantSeed(selectedRecipe, GameManager.Instance.currentDay);

        // 3. UIを閉じる
        CloseSeedList();
    }
}