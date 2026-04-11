using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MainConsole : MonoBehaviour, IInteractable
{
    public static MainConsole Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==========================================
    // 機能1：ツリー形式のアップデート（自己完結）
    // ==========================================
    [System.Serializable]
    public class TechTreeNode
    {
        public string upgradeID;          // 例："Engine_Speed_1"
        public string displayName;        // 例："エンジン出力強化Lv1"
        [TextArea] public string description;
        
        public string requiredPreviousID; // 前提となるアップグレードID（空なら最初から解放可能）
        public List<ItemRequirement> requirements; // 必要な素材
        
        public bool isUnlocked = false;   // 解放済みか？
    }

    [Header("【1】アップデートツリー設定")]
    public List<TechTreeNode> upgradeTree = new List<TechTreeNode>();

    // アップグレードを実行する処理
    public bool TryUnlockUpgrade(string id)
    {
        TechTreeNode node = upgradeTree.Find(n => n.upgradeID == id);
        if (node == null || node.isUnlocked) return false;

        // 前提条件のチェック
        if (!string.IsNullOrEmpty(node.requiredPreviousID))
        {
            TechTreeNode prevNode = upgradeTree.Find(n => n.upgradeID == node.requiredPreviousID);
            if (prevNode == null || !prevNode.isUnlocked)
            {
                Debug.LogWarning("前提となるアップグレードが解放されていません。");
                return false;
            }
        }

        // 素材のチェックと消費
        if (!HasRequiredItems(node.requirements)) return false;
        ConsumeItems(node.requirements);

        node.isUnlocked = true;
        Debug.Log($"アップグレード完了：{node.displayName}");
        
        // （ここに潜水艦のステータスを上げる処理を書く）
        return true;
    }


    // ==========================================
    // 機能2：サンプル装備スロット（自己完結）
    // ==========================================
    [System.Serializable]
    public class EquipmentSlot
    {
        public string slotName; // 例："メインジェネレーター", "サブフィルター"
        public ItemData equippedSample; // 現在装備中のサンプル（nullなら空き）
    }

    [Header("【2】装備スロット設定")]
    public List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>();

    // サンプルを装備する処理
    public void EquipSample(int slotIndex, ItemData sampleToEquip)
    {
        if (slotIndex < 0 || slotIndex >= equipmentSlots.Count) return;

        // すでに装備されているものがあれば、インベントリに返す
        if (equipmentSlots[slotIndex].equippedSample != null)
        {
            InventoryManager.Instance.AddItem(equipmentSlots[slotIndex].equippedSample);
        }

        // インベントリから装備するサンプルを減らし、スロットにセット
        InventoryManager.Instance.RemoveItem(sampleToEquip);
        equipmentSlots[slotIndex].equippedSample = sampleToEquip;
        
        Debug.Log($"{equipmentSlots[slotIndex].slotName} に {sampleToEquip.itemName} を装備しました。");
        
        // （ここに装備によるバフ効果を適用する処理を書く）
    }


    // ==========================================
    // 機能3：メインミッション提出（GameManager連携）
    // ==========================================
    [System.Serializable]
    public class MissionSubmission
    {
        public string missionTitle;
        public List<ItemRequirement> requirements;
        public string targetFlagName; // クリア時にONにするGameManagerのフラグ
        public bool isSubmitted = false;
    }

    [Header("【3】ミッション提出設定")]
    public List<MissionSubmission> missionSubmissions = new List<MissionSubmission>();

    // ミッションのアイテムを提出する処理
    public bool TrySubmitMission(string flagName)
    {
        MissionSubmission mission = missionSubmissions.Find(m => m.targetFlagName == flagName);
        if (mission == null || mission.isSubmitted) return false;

        // 素材のチェックと消費
        if (!HasRequiredItems(mission.requirements)) return false;
        ConsumeItems(mission.requirements);

        mission.isSubmitted = true;
        
        // ★ここだけ GameManager に干渉する！★
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag(mission.targetFlagName, true);
        }

        Debug.Log($"ミッション達成！フラグ起動：{mission.targetFlagName}");
        return true;
    }


    // ==========================================
    // 共通の便利メソッド群
    // ==========================================
    private bool HasRequiredItems(List<ItemRequirement> reqs)
    {
        foreach (var req in reqs)
        {
            if (InventoryManager.Instance.GetItemCount(req.item) < req.amount) return false;
        }
        return true;
    }

    private void ConsumeItems(List<ItemRequirement> reqs)
    {
        foreach (var req in reqs)
        {
            InventoryManager.Instance.RemoveItems(req.item, req.amount);
        }
    }

    // プレイヤーがアクセスした時
    public string GetInteractPrompt() => "メインコンソールを起動する";

    public void Interact()
    {
        // 💡 ここで3つの機能を持った「大型UI」を開く処理を呼び出す
        Debug.Log("メインコンソールを開きました！");
        // MainConsoleUI.Instance.OpenUI();
    }
}