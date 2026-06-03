using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class EquipmentUI : MonoBehaviour
{
    [Header("スロット一覧エリア（左側）")]
    public Transform slotContainer;
    public GameObject slotButtonPrefab;

    [Header("所持アイテム一覧エリア（右側）")]
    public Transform inventoryContainer;
    public GameObject inventoryButtonPrefab;
    public TextMeshProUGUI selectedSlotDescText;

    private int currentSlotIndex = -1;

    // タブが開かれた時に MainConsoleUI から呼ばれる
    public void InitializeEquipTab()
    {
        currentSlotIndex = -1;
        if (selectedSlotDescText != null) selectedSlotDescText.text = "スロットを選択してください";
        ClearContainer(inventoryContainer);
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        ClearContainer(slotContainer);
        if (MainConsole.Instance == null) return;

        for (int i = 0; i < MainConsole.Instance.equipmentSlots.Count; i++)
        {
            int index = i; // 💡 クロージャ（コールバック用）の変数キャプチャ対策
            var slotData = MainConsole.Instance.equipmentSlots[i];

            GameObject btnObj = Instantiate(slotButtonPrefab, slotContainer);
            
            // プレハブ内にTextが2つある想定（[0]スロット名, [1]装備中のアイテム名）
            TextMeshProUGUI[] texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                texts[0].text = slotData.slotName;
                
                if (slotData.equippedSample != null)
                    texts[1].text = $"<color=#00FF00>{slotData.equippedSample.itemName}</color>";
                else
                    texts[1].text = "<color=#888888>未装備</color>";
            }

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSlotClicked(index));
        }
    }

    private void OnSlotClicked(int index)
    {
        currentSlotIndex = index;
        var slotData = MainConsole.Instance.equipmentSlots[index];
        if (selectedSlotDescText != null) selectedSlotDescText.text = $"【{slotData.slotName}】\n装備するサンプルを選んでください";

        RefreshInventoryList();
    }

    private void RefreshInventoryList()
    {
        ClearContainer(inventoryContainer);
        if (currentSlotIndex < 0) return;

        var slotData = MainConsole.Instance.equipmentSlots[currentSlotIndex];

        // 💡 もし既に何か装備していたら、一番上に「外す」ボタンを出す
        if (slotData.equippedSample != null)
        {
            CreateInventoryButton("<color=red>装備を外す</color>", null);
        }

        // インベントリから「装備可能なアイテム（今回はCategoryがSampleのもの）」を抽出して重複なしリストにする
        var equipableItems = InventoryManager.Instance.inventoryList
            .Where(item => item.category == ItemCategory.Sample)
            .GroupBy(item => item)
            .Select(g => g.Key)
            .ToList();

        if (equipableItems.Count == 0 && slotData.equippedSample == null)
        {
            if (selectedSlotDescText != null) selectedSlotDescText.text += "\n\n<color=orange>※装備可能なサンプルを持っていません</color>";
        }

        foreach (var item in equipableItems)
        {
            // 所持数も表示してあげる
            int count = InventoryManager.Instance.GetItemCount(item);
            CreateInventoryButton($"{item.itemName} (所持: {count})", item);
        }
    }

    private void CreateInventoryButton(string displayText, ItemData item)
    {
        GameObject btnObj = Instantiate(inventoryButtonPrefab, inventoryContainer);
        TextMeshProUGUI textUI = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textUI != null) textUI.text = displayText;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() => 
        {
            if (item == null)
            {
                // 外す処理
                var slotData = MainConsole.Instance.equipmentSlots[currentSlotIndex];
                InventoryManager.Instance.AddItem(slotData.equippedSample);
                slotData.equippedSample = null;
            }
            else
            {
                // 装備する処理（MainConsoleのメソッドを呼ぶ）
                MainConsole.Instance.EquipSample(currentSlotIndex, item);
            }
            
            // 装備・脱着が終わったら画面を再描画
            RefreshSlots();
            RefreshInventoryList();
        });
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container) Destroy(child.gameObject);
    }
}