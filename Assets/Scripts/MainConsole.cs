using UnityEngine;
using System.Collections.Generic;

public class MainConsole : MonoBehaviour, IInteractable
{
    // シングルトン化（どこからでも MainConsole.Instance でアクセスできるようにする）
    public static MainConsole Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==========================================
    // 機能1：アップグレードツリー
    // ==========================================
    [System.Serializable]
    public class ItemRequirement
    {
        public ItemData item;   // 必要な素材アイテム
        public int amount;      // 必要な個数
    }

    [System.Serializable]
    public class TechTreeNode
    {
        [Header("基本設定")]
        public string upgradeID;          // プログラム用の一意のID（例: "Hull_Lv1"）
        public string displayName;        // UIに表示する名前
        [TextArea] public string description; // UIに表示する説明文
        public string requiredPreviousID; // これを解放するために必要な1つ前のID（空なら初期ノード）
        public Vector2 uiPosition;        // UI上の配置座標

        [Header("解放条件")]
        public List<ItemRequirement> requirements; // 必要な素材リスト

        [Header("解放状態")]
        public bool isUnlocked = false;   // 解放済みかどうか

        [Header("解放時に発動する効果")]
        public List<SubmarineEffect> upgradeEffects = new List<SubmarineEffect>(); // 新パイプライン！
    }

    [Header("【1】アップデートツリー設定")]
    public List<TechTreeNode> upgradeTree = new List<TechTreeNode>();

    public void Interact()
    {
        if (MainConsoleUI.Instance != null)
        {
            MainConsoleUI.Instance.OpenConsole();
        }
    }

    public string GetInteractPrompt()
    {
        return "コンソールを操作する";
    }
    public bool TryUnlockUpgrade(string upgradeID)
    {
        var node = upgradeTree.Find(n => n.upgradeID == upgradeID);
        
        // 存在しない、または既に解放済みなら失敗
        if (node == null || node.isUnlocked) return false;

        // ① 前提条件のチェック
        if (!string.IsNullOrEmpty(node.requiredPreviousID))
        {
            var prevNode = upgradeTree.Find(n => n.upgradeID == node.requiredPreviousID);
            if (prevNode == null || !prevNode.isUnlocked)
            {
                Debug.LogWarning("前提アップグレードが解放されていません。");
                return false;
            }
        }

        // ② 素材の所持チェック
        foreach (var req in node.requirements)
        {
            if (InventoryManager.Instance.GetItemCount(req.item) < req.amount)
            {
                Debug.LogWarning($"{req.item.itemName} が足りません。");
                return false;
            }
        }

        // ③ 素材の消費（すべて揃っていた場合のみここに来る）
        foreach (var req in node.requirements)
        {
            // GetItemCountとRemoveItemの仕様に合わせて1個ずつ減らす
            for (int i = 0; i < req.amount; i++)
            {
                InventoryManager.Instance.RemoveItem(req.item);
            }
        }

        // ④ 解放フラグを立てて、効果を適用する
        node.isUnlocked = true;
        ApplyUpgradeEffect(node);
        
        Debug.Log($"【解放完了】{node.displayName} がシステムにインストールされました！");
        return true;
    }

    // 💡 新パイプライン：アップグレード効果の適用
    private void ApplyUpgradeEffect(TechTreeNode node)
    {
        if (SubmarineStatus.Instance == null || node == null) return;

        // SubmarineStatus の一括処理メソッドに丸投げ！（true = 適用する）
        SubmarineStatus.Instance.ApplyEffects(node.upgradeEffects, true);
        Debug.Log($"メインコンソール：アップグレード『{node.displayName}』の効果を一括適用しました。");
    }

    // ==========================================
    // 機能2：サンプル装備スロット
    // ==========================================
    [System.Serializable]
    public class EquipmentSlot
    {
        public string slotName;               // スロット名（例: "メインエンジン"）
        public EquipSlotType requiredType;    // このスロットにはめられる装備の種類を指定
        public ItemData equippedSample;       // 現在セットされているアイテム
    }

    [Header("【2】装備スロット設定")]
    public List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>();

    // 💡 装備・または外す処理（UIから呼ばれる）
    public void EquipSample(int slotIndex, ItemData sampleToEquip)
    {
        if (slotIndex < 0 || slotIndex >= equipmentSlots.Count) return;
        var slot = equipmentSlots[slotIndex];

        // 1. もし既に何か装備していたら、効果を消してインベントリに戻す
        if (slot.equippedSample != null)
        {
            // インベントリにアイテムを返却
            InventoryManager.Instance.AddItem(slot.equippedSample);
            
            // 新パイプライン：効果を解除（false = マイナスする）
            if (SubmarineStatus.Instance != null)
            {
                SubmarineStatus.Instance.ApplyEffects(slot.equippedSample.equipEffects, false);
            }
            
            slot.equippedSample = null;
        }

        // 2. 新しいアイテムを装備する場合
        if (sampleToEquip != null)
        {
            // インベントリからアイテムを消費
            InventoryManager.Instance.RemoveItem(sampleToEquip);
            
            // スロットにセット
            slot.equippedSample = sampleToEquip;
            
            // 新パイプライン：効果を適用（true = プラスする）
            if (SubmarineStatus.Instance != null)
            {
                SubmarineStatus.Instance.ApplyEffects(sampleToEquip.equipEffects, true);
            }
            
            Debug.Log($"【装備完了】{slot.slotName} に {sampleToEquip.itemName} をセットしました。");
        }
    }

    // ==========================================
    // 機能3：ミッション提出（ストーリー進行）システム
    // ==========================================
    [System.Serializable]
    public class SubmissionMission
    {
        public string missionID;          // 進行管理用のID
        public string displayText;        // UIに表示する任務名
        public List<ItemRequirement> requiredItems; // 要求されるアイテムリスト
    }

    [Header("【3】ミッション提出設定")]
    public List<SubmissionMission> submissionMissions = new List<SubmissionMission>();

    // 💡 現在アクティブなミッションを取得する
    public SubmissionMission GetCurrentActiveSubmissionMission()
    {
        // ※今回はシンプルに、リストの一番上にあるものを「現在の目標」として返す
        if (submissionMissions.Count > 0)
        {
            return submissionMissions[0];
        }
        return null;
    }

    // 💡 ミッションの提出を試みる
    public bool TrySubmitMission(SubmissionMission mission)
    {
        if (mission == null) return false;

        // 1. 素材が足りているかチェック
        foreach (var req in mission.requiredItems)
        {
            if (InventoryManager.Instance.GetItemCount(req.item) < req.amount) return false;
        }

        // 2. 素材を消費する
        foreach (var req in mission.requiredItems)
        {
            for (int i = 0; i < req.amount; i++)
            {
                InventoryManager.Instance.RemoveItem(req.item);
            }
        }

        // 3. 提出完了処理
        Debug.Log($"【任務完了】ミッション『{mission.displayText}』を達成しました！");
        
        // 達成したミッションをリストから削除（次のミッションがリストの先頭になる）
        submissionMissions.Remove(mission);

        // ※将来的にここで GameManager.Instance.AdvancePhase(); などを呼んでストーリーを進めます

        return true;
    }
}