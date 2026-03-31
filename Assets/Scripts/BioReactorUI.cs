using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

public class BioReactorUI : MonoBehaviour
{
    [Header("UIのON/OFF制御")]
    public GameObject mainWindow; 
    public GameObject ingredientScrollView; 

    [Header("UI参照")]
    public Transform ingredientListParent; 
    public GameObject ingredientButtonPrefab; 

    [Tooltip("素材リストを開くためのボタン")]
    public GameObject ingredientToggleButton;

    [Header("工程ボタン")]
    public GameObject extractionButton;
    public GameObject startSpinButton;

    [Header("3D落下ギミック")]
    public Transform dropPoint; 
    public GameObject defaultDropPrefab;

    private Action onCloseCallback;

   public void OpenReactorUI(Action onClose = null)
    {
        onCloseCallback = onClose;

        if (mainWindow != null) mainWindow.SetActive(true);
        
        if (ingredientScrollView != null) ingredientScrollView.SetActive(false); 
        SetExtractionButtonVisible(false);
        SetStartSpinButtonVisible(false);
    }

    public void UpdateUIState(float liquidAmount, bool isAllPipettesFull, bool isCentrifuging)
    {
        // 💡 液体が少しでも（0.1f以上）入っていれば、素材投入ボタンを表示する
        if (ingredientToggleButton != null)
        {
            ingredientToggleButton.SetActive(liquidAmount > 0.1f && !isCentrifuging);
        }
        
        // 抽出ボタンの表示条件（ピペット満タン ＆ まだ移動前）
        SetExtractionButtonVisible(isAllPipettesFull && !isCentrifuging);
    }
    public void SetExtractionButtonVisible(bool isVisible)
    {
        if (extractionButton != null) extractionButton.SetActive(isVisible);
    }

    public void SetStartSpinButtonVisible(bool isVisible)
    {
        if (startSpinButton != null) startSpinButton.SetActive(isVisible);
    }

    public void ToggleIngredientList()
    {
        if (ingredientScrollView != null)
        {
            bool isActive = ingredientScrollView.activeSelf;
            ingredientScrollView.SetActive(!isActive);

            if (!isActive) RefreshIngredientList();
        }
    }

    public void RefreshIngredientList()
    {
        foreach (Transform child in ingredientListParent) Destroy(child.gameObject);

        if (FlaskReceiver.Instance.currentLiquidAmount <= 0f)
        {
            GameObject msgObj = Instantiate(ingredientButtonPrefab, ingredientListParent);
            TextMeshProUGUI textUI = msgObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textUI != null) textUI.text = "<color=#FF6666>液体を先に注いでください</color>";
            
            Button btn = msgObj.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
            return;
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
            if (btn != null) btn.onClick.AddListener(() => OnIngredientClicked(item));
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
                GameObject droppedObj = Instantiate(prefabToDrop, dropPoint.position, UnityEngine.Random.rotation);
                IngredientObject ingredientScript = droppedObj.GetComponent<IngredientObject>();
                if (ingredientScript != null) ingredientScript.ingredientData = item; 
            }
            else
            {
                FlaskReceiver.Instance.AddIngredient(item);
            }
        }
    }

    public void CloseUI()
    {
        if (mainWindow != null) mainWindow.SetActive(false);
        if (ingredientScrollView != null) ingredientScrollView.SetActive(false);
        
        SetExtractionButtonVisible(false);
        SetStartSpinButtonVisible(false);

        onCloseCallback?.Invoke();
        onCloseCallback = null;

        if (GameManager.Instance != null) GameManager.Instance.UnlockPlayer();
        if (UIManager.Instance != null) UIManager.Instance.SetDialogueMode(false); 
        if (UIManager.Instance != null) UIManager.Instance.SetHUDVisible(true);
    }
}