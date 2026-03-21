using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro; // 文字用

public class ExtractionUIManager : MonoBehaviour
{
    public static ExtractionUIManager Instance;

    [Header("全体パネル")]
    public GameObject extractionPanel; 

    [Header("リスト表示用")]
    [Tooltip("アイテムを並べる親オブジェクト（Scroll ViewのContentなど）")]
    public Transform contentParent; 
    public GameObject itemSlotPrefab; // アイテム1個分のUIプレハブ

    [Header("詳細表示（マウスオーバー用）")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipNameText;
    public TextMeshProUGUI tooltipDescriptionText;

    private Action onCloseCallback;
    private List<ItemData> currentItems = new List<ItemData>();

    private void Awake()
    {
        Instance = this;
        if (extractionPanel != null) extractionPanel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    // コンテナから呼ばれるメソッド
    public void OpenExtractionUI(List<ItemData> items, Action onClose)
    {
        currentItems = items;
        onCloseCallback = onClose;

        // まず、前回開いた時の古いUIスロットを全部お掃除する
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 今回回収したアイテムを順番にUIとして生成する
        foreach (var item in items)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, contentParent);
            ExtractionItemSlot slotScript = slotObj.GetComponent<ExtractionItemSlot>();
            
            if (slotScript != null)
            {
                slotScript.Setup(item, this);
            }
        }

        // パネルを表示して、プレイヤーのカーソルを出す（UIManagerの機能を利用）
        extractionPanel.SetActive(true);
        if (GameManager.Instance != null) GameManager.Instance.LockPlayer(); 
        if (UIManager.Instance != null) UIManager.Instance.SetHUDVisible(false); 
        
    }


   public void TakeAllItems()
    {
        if (InventoryManager.Instance != null)
        {
            foreach (var item in currentItems)
            {
                InventoryManager.Instance.AddItem(item);
            }
        }
        else
        {
            Debug.LogError("InventoryManagerが見つかりません！シーンに存在するか確認してください。");
        }

        Debug.Log($"合計 {currentItems.Count} 個のアイテムをインベントリに送りました！");
        
        CloseUI();
    }

    // パネルを閉じる処理
    public void CloseUI()
    {
        extractionPanel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        // FPSモードに戻す
        if (GameManager.Instance != null) GameManager.Instance.UnlockPlayer();
        if (UIManager.Instance != null) UIManager.Instance.SetHUDVisible(true);

        // コンテナ側に「終わったよ！」と報告する
        onCloseCallback?.Invoke();
        onCloseCallback = null;
    }

    // ==========================================
    // マウスオーバー時の詳細表示機能
    // ==========================================
    public void ShowTooltip(ItemData item)
    {
        if (tooltipPanel == null || item == null) return;
        
        tooltipNameText.text = item.itemName;
        tooltipDescriptionText.text = item.description; 
        
        // （後々、ここに「遺伝子データ: 〇〇」などのタグ情報も表示できます！）

        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}