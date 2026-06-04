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

        [Header("UI上の配置座標")]
        public Vector2 uiPosition; 
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
        
        ApplyUpgradeEffect(node.upgradeID);

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

    private void ApplyUpgradeEffect(string id)
    {
        if (SubmarineStatus.Instance == null) return;

        switch (id)
        {
            // ─── ステータス強化系 ───
            case "Hull_Lv1":
                SubmarineStatus.Instance.maxHP += 50f;
                SubmarineStatus.Instance.currentHP += 50f; // 上限アップと同時に回復
                Debug.Log("【強化】装甲Lv1：最大HPが +50 されました！");
                break;
                
            case "Engine_Lv1":
                SubmarineStatus.Instance.speedMultiplier += 0.2f; // 速度 +20%
                Debug.Log("【強化】エンジンLv1：移動速度が 20% アップしました！");
                break;

            case "Steering_Lv1":
                SubmarineStatus.Instance.turnMultiplier += 0.5f; // 旋回力 +50%
                Debug.Log("【強化】操舵Lv1：旋回速度が 50% アップしました！");
                break;

            // ─── スキル解放系（将来用） ───
            case "Skill_Turbo":
                SubmarineStatus.Instance.canUseTurbo = true;
                Debug.Log("【スキル解放】ターボブースト機能がシステムにインストールされました！");
                break;

            case "Skill_DeepSonar":
                SubmarineStatus.Instance.canUseDeepSonar = true;
                Debug.Log("【スキル解放】深海探査用ソナーが使用可能になりました！");
                break;

            default:
                Debug.LogWarning($"未設定のアップグレードIDです: {id}");
                break;
        }
    }


    public GameManager.MissionObjective GetCurrentActiveSubmissionMission()
    {
        if (GameManager.Instance == null) return null;

        foreach (var mission in GameManager.Instance.missionList)
        {
            // 「コンソール提出が必要」かつ「まだ未クリア」かつ「出現条件（日数・フラグ）を満たしている」ものを探す
            if (!mission.requiresConsoleSubmission) continue;

            // クリア済みか判定
            bool isCleared = mission.targetFlagNames.Count > 0;
            foreach (string flagName in mission.targetFlagNames)
            {
                if (!GameManager.Instance.GetFlag(flagName)) { isCleared = false; break; }
            }

            if (isCleared) continue; // クリア済みならパス

            // 出現条件の判定（GameManagerのHUD更新ロジックと同じ）
            bool isAppearFlagSet = string.IsNullOrEmpty(mission.requiredFlagToAppear) || GameManager.Instance.GetFlag(mission.requiredFlagToAppear);
            bool isTimeMet = true;
            if (mission.appearDay > 0)
            {
                if (GameManager.Instance.currentDay < mission.appearDay) isTimeMet = false;
                else if (GameManager.Instance.currentDay == mission.appearDay && GameManager.Instance.currentPhase < mission.appearPhase) isTimeMet = false;
            }
            else
            {
                if (GameManager.Instance.currentPhase < mission.appearPhase) isTimeMet = false;
            }

            // すべての表示条件を満たしている任務があれば、それを「今提出すべきミッション」として返す
            if (isAppearFlagSet && isTimeMet)
            {
                return mission;
            }
        }
        return null; // 対象のミッションが今は無い
    }

    // ミッションのアイテムを提出する処理
    public bool TrySubmitMission(GameManager.MissionObjective mission)
    {
        if (mission == null) return false;

        // 素材のチェック
        if (!HasRequiredItems(mission.requiredItems)) return false;
        
        // 素材の消費
        ConsumeItems(mission.requiredItems);

        // ★ GameManager側のクリアフラグをすべてONにする！
        if (GameManager.Instance != null)
        {
            foreach (string flag in mission.targetFlagNames)
            {
                GameManager.Instance.SetFlag(flag, true);
            }
            // ログやHUDの再更新をかける
            GameManager.Instance.UpdateMainMissionHUD();
        }

        Debug.Log($"コンソール：ミッション『{mission.displayText}』の素材提出が完了しました！");
        return true;
    }

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

    public string GetInteractPrompt() => "メインコンソールを起動する";

    public void Interact()
    {
        if (MainConsoleUI.Instance != null)
        {
            MainConsoleUI.Instance.OpenConsole();
        }
    }
    
}

