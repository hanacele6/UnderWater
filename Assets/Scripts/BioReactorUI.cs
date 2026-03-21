using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class BioReactorUI : MonoBehaviour
{
    [Header("UI参照")]
    public Transform ingredientListParent; // 素材ボタンを並べる場所
    public GameObject ingredientButtonPrefab; // 素材ボタンのプレハブ

    // 作業机（調合画面）を開いた時に呼ぶ
    public void OpenReactorUI()
    {
        RefreshIngredientList();
    }

    // 持っている「素材」だけをリストアップする
    public void RefreshIngredientList()
    {
        // 古いボタンを削除
        foreach (Transform child in ingredientListParent)
        {
            Destroy(child.gameObject);
        }

        if (InventoryManager.Instance == null) return;

        // ==========================================
        // ★大活躍！カテゴリが「Material（素材）」のものだけを抽出してまとめる
        // ==========================================
        var materials = InventoryManager.Instance.inventoryList
            .Where(item => item.category == ItemCategory.Material)
            .GroupBy(item => item)
            .ToList();

        foreach (var group in materials)
        {
            ItemData item = group.Key;
            int count = group.Count();

            GameObject btnObj = Instantiate(ingredientButtonPrefab, ingredientListParent);
            TextMeshProUGUI textUI = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (textUI != null)
            {
                textUI.text = $"{item.itemName} <color=#FFFF00>x{count}</color>";
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                // ボタンを押した時の処理をセット！
                btn.onClick.AddListener(() => OnIngredientClicked(item));
            }
        }
    }

    // 素材ボタンがクリックされた時の処理
    private void OnIngredientClicked(ItemData item)
    {
        // 1. 鍋（SynthesisCauldron）にアイテムのデータを送る
        FlaskReceiver.Instance.AddIngredient(item);

        // 2. インベントリからそのアイテムを1つ消す
        InventoryManager.Instance.RemoveItem(item);

        // 3. リストの個数表示を更新する
        RefreshIngredientList();
    }
}