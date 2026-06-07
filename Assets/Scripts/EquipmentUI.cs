using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems; // ホバー検知用

public class EquipmentUI : MonoBehaviour
{
    [Header("UIエリア")]
    public Transform slotContainer;
    public Transform inventoryContainer;
    
    [Header("右側の詳細エリア")]
    public TextMeshProUGUI detailText;
    // 💡 ActionButton関連の変数はバッサリ削除しました！

    [Header("プレハブ")]
    public GameObject slotButtonPrefab;
    public GameObject inventoryButtonPrefab;

    private int currentSlotIndex = -1;

    public void InitializeEquipTab()
    {
        currentSlotIndex = -1;
        ShowDetail("装備システム", "左側のリストから調整したいスロットを選択してください。");
        ClearContainer(inventoryContainer);
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        ClearContainer(slotContainer);
        if (MainConsole.Instance == null) return;

        for (int i = 0; i < MainConsole.Instance.equipmentSlots.Count; i++)
        {
            int index = i; 
            var slotData = MainConsole.Instance.equipmentSlots[i];

            GameObject btnObj = Instantiate(slotButtonPrefab, slotContainer);
            TextMeshProUGUI[] texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
            
            if (texts.Length >= 2)
            {
                texts[0].text = slotData.slotName;
                texts[1].text = slotData.equippedSample != null ? $"<color=#00FF00>{slotData.equippedSample.itemName}</color>" : "<color=#888888>未装備</color>";
            }

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSlotClicked(index));

            string desc = $"【要求タイプ】\n{slotData.requiredType}\n\n";
            if (slotData.equippedSample != null) desc += $"現在装備中：{slotData.equippedSample.itemName}\n【効果】\n{GetEffectsString(slotData.equippedSample.equipEffects)}";
            else desc += "現在何も装備されていません。";
            
            AddHoverEvent(btnObj, $"スロット: {slotData.slotName}", desc);
        }
    }

    private void OnSlotClicked(int index)
    {
        currentSlotIndex = index;
        ShowDetail("スロット選択中", "真ん中のリストから、装備したいアイテムを\n<color=orange>『クリックして直接装備』</color>してください。");
        RefreshInventoryList();
    }

    private void RefreshInventoryList()
    {
        ClearContainer(inventoryContainer);
        if (currentSlotIndex < 0) return;

        var slotData = MainConsole.Instance.equipmentSlots[currentSlotIndex];

        // 「外す」ボタンの生成
        if (slotData.equippedSample != null)
        {
            GameObject btnObj = Instantiate(inventoryButtonPrefab, inventoryContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = "<color=red>装備を外す</color>";
            
            AddHoverEvent(btnObj, "装備解除", "クリックすると現在装備しているアイテムを外し、\nインベントリに戻します。");
            
            btnObj.GetComponent<Button>().onClick.AddListener(() => 
            {
                // 💡 クリック即解除！
                MainConsole.Instance.EquipSample(currentSlotIndex, null); 
                RefreshSlots();
                RefreshInventoryList();
                ShowDetail("システム更新", "装備を外しました。");
            });
        }

        // 装備可能なアイテムリストの生成
        var equipableItems = InventoryManager.Instance.inventoryList
            .Where(item => item.equipType == slotData.requiredType && item.equipType != EquipSlotType.None)
            .GroupBy(item => item)
            .Select(g => g.Key)
            .ToList();

        foreach (var item in equipableItems)
        {
            int count = InventoryManager.Instance.GetItemCount(item);
            GameObject btnObj = Instantiate(inventoryButtonPrefab, inventoryContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = $"{item.itemName} (所持: {count})";
            
            AddHoverEvent(btnObj, item.itemName, $"{item.description}\n\n<color=yellow>【装備効果】\n{GetEffectsString(item.equipEffects)}</color>\n\n<color=white>▶ クリックで装備する</color>");

            btnObj.GetComponent<Button>().onClick.AddListener(() => 
            {
                // 💡 クリック即装備！
                MainConsole.Instance.EquipSample(currentSlotIndex, item); 
                RefreshSlots();
                RefreshInventoryList();
                ShowDetail("システム更新", $"{item.itemName} を装備しました。");
            });
        }
    }

    // --- 補助メソッド ---
    private void ShowDetail(string title, string desc)
    {
        if (detailText != null) detailText.text = $"<size=120%><color=yellow>【{title}】</color></size>\n\n{desc}";
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container) Destroy(child.gameObject);
    }

    private void AddHoverEvent(GameObject obj, string title, string desc)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = obj.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        // 前回のエラー対策のフルパスもそのまま入れています
        entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter; 
        entry.callback.AddListener((data) => { ShowDetail(title, desc); });
        trigger.triggers.Add(entry);
    }

    // 💡 新パイプラインのエフェクトリストをUI用の文字列に変換するメソッド
    private string GetEffectsString(List<SubmarineEffect> effects)
    {
        if (effects == null || effects.Count == 0) return "効果なし";
        
        string result = "";
        foreach (var eff in effects)
        {
            result += $"・{eff.effectType} : {eff.value}\n";
        }
        return result.TrimEnd(); // 最後の改行を削る
    }
}