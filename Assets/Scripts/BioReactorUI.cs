using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

public class BioReactorUI : MonoBehaviour
{
    [Header("UIのON/OFF制御")]
    [Tooltip("実験机の背景パネル本体（Canvasの直下にあるPanel）")]
    public GameObject mainWindow; 
    [Tooltip("素材リストのScroll View本体（ボタンを押すまで隠しておく）")]
    public GameObject ingredientScrollView; 

    [Header("UI参照")]
    public Transform ingredientListParent; 
    public GameObject ingredientButtonPrefab; 

    [Header("3D落下ギミック")]
    [Tooltip("フラスコ上空の落下地点（空オブジェクトをアサイン）")]
    public Transform dropPoint; 
    [Tooltip("アイテム専用モデルがない時に落とす汎用プレハブ")]
    public GameObject defaultDropPrefab;


    private Action onCloseCallback;

    public void OpenReactorUI(Action onClose = null)
    {
        onCloseCallback = onClose;

        if (mainWindow != null) mainWindow.SetActive(true);
        if (ingredientScrollView != null) ingredientScrollView.SetActive(false); 
    }


    public void ToggleIngredientList()
    {
        if (ingredientScrollView != null)
        {
            // 今の状態の「逆」にする（開いていたら閉じ、閉じていたら開く）
            bool isActive = ingredientScrollView.activeSelf;
            ingredientScrollView.SetActive(!isActive);

            // 開いた時だけ中身を最新にする
            if (!isActive)
            {
                RefreshIngredientList();
            }
        }
    }

    // 持っている「素材」だけをリストアップする
    public void RefreshIngredientList()
    {
        foreach (Transform child in ingredientListParent)
        {
            Destroy(child.gameObject);
        }

        if (InventoryManager.Instance == null) return;

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
            
            if (textUI != null) textUI.text = $"{item.itemName} <color=#FFFF00>x{count}</color>";

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnIngredientClicked(item));
            }
        }
    }

    private void OnIngredientClicked(ItemData item)
    {
        InventoryManager.Instance.RemoveItem(item);
        RefreshIngredientList(); 

        if (dropPoint != null)
        {
            GameObject prefabToDrop = item.dropPrefab != null ? item.dropPrefab : defaultDropPrefab;

            if (prefabToDrop != null)
            {
                // 決定したプレハブを、フラスコの上空にランダムな角度で生成
                GameObject droppedObj = Instantiate(prefabToDrop, dropPoint.position, UnityEngine.Random.rotation);
                
                // 落下物に「君はこのアイテムのデータだよ」と教え込む
                IngredientObject ingredientScript = droppedObj.GetComponent<IngredientObject>();
                if (ingredientScript != null)
                {
                    ingredientScript.ingredientData = item; 
                }
            }
            else
            {
                Debug.LogWarning($"{item.itemName} の落下プレハブがないため、直接フラスコに投入しました。");
                FlaskReceiver.Instance.AddIngredient(item);
            }
        }
    }

    public void CloseUI()
    {
        if (mainWindow != null) mainWindow.SetActive(false);
        if (ingredientScrollView != null) ingredientScrollView.SetActive(false);


        onCloseCallback?.Invoke();
        onCloseCallback = null;

        if (GameManager.Instance != null) GameManager.Instance.UnlockPlayer();

        if (UIManager.Instance != null) UIManager.Instance.SetDialogueMode(false); 
        if (UIManager.Instance != null) UIManager.Instance.SetHUDVisible(true);
    }
}