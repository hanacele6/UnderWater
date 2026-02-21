using UnityEngine;
using System.Collections.Generic;

public class NPCController : MonoBehaviour, IInteractable
{
    // ==========================================
    // 会話の分岐ルールを定義する構造体
    // ==========================================
    [System.Serializable]
    public struct DialogueBranch
    {
        public string memo; // インスペクター整理用のメモ（例：「3日目・鍵あり時」）

        [Header("発生条件")]
        public GamePhase targetPhase;     // どのフェーズで発生するか
        
        [Tooltip("指定した日数【以降】に発生します（0ならいつでも）")]
        public int requiredDay;           
        
        [Tooltip("必要なフラグ名（空欄ならフラグ条件なし）")]
        public string requiredFlag;       
        
        [Tooltip("チェックを入れるとフラグON、外すとフラグOFFが条件になります")]
        public bool requireFlagTrue;      

        [Header("再生する会話データ")]
        public DialogueData firstTalk;    // 初回
        public DialogueData repeatedTalk; // 2回目以降（空欄でもOK）
    }

    [Header("会話の設定")]
    [Tooltip("★重要：リストの【上から順】に条件を満たしているかチェックされます！特殊な条件ほど上に置いてください。")]
    public List<DialogueBranch> dialogueBranches = new List<DialogueBranch>();
    
    [Tooltip("どの条件にも当てはまらない時のデフォルト会話")]
    public DialogueData defaultDialogue; 

    private bool hasTalkedThisPhase = false;
    private GamePhase lastTalkedPhase;
    private int lastTalkedDay;

    public string GetInteractPrompt()
    {
        return "話しかける";
    }

    public void Interact()
    {
        // GameManagerから現在の状況を取得
        if (GameManager.Instance == null) return;
        GamePhase currentPhase = GameManager.Instance.currentPhase;
        int currentDay = GameManager.Instance.currentDay;

        // 日付かフェーズが変わったら、「もう話した」という記憶をリセット
        if (lastTalkedPhase != currentPhase || lastTalkedDay != currentDay)
        {
            hasTalkedThisPhase = false;
            lastTalkedPhase = currentPhase;
            lastTalkedDay = currentDay;
        }

        DialogueData dialogueToPlay = defaultDialogue;

        // ==========================================
        // リストを上から順番にチェックして、会話を決定する
        // ==========================================
        foreach (var branch in dialogueBranches)
        {
            // 1. フェーズが一致しているか？
            if (branch.targetPhase != currentPhase) continue;

            // 2. 日数が条件を満たしているか？（0より大きい数値が設定されている場合のみチェック）
            if (branch.requiredDay > 0 && currentDay < branch.requiredDay) continue;

            // 3. フラグが条件を満たしているか？（文字が入力されている場合のみチェック）
            if (!string.IsNullOrEmpty(branch.requiredFlag))
            {
                bool actualFlagState = GameManager.Instance.GetFlag(branch.requiredFlag);
                // 実際のフラグ状態と、要求されている状態（ONかOFFか）が違ったらスキップ
                if (actualFlagState != branch.requireFlagTrue) continue; 
            }

            // ★すべての条件をクリアした！この会話を採用して検索終了
            if (hasTalkedThisPhase && branch.repeatedTalk != null)
            {
                dialogueToPlay = branch.repeatedTalk;
            }
            else
            {
                dialogueToPlay = branch.firstTalk;
            }
            break; 
        }

        // ==========================================
        // 会話の再生
        // ==========================================
        if (dialogueToPlay != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueToPlay);
            hasTalkedThisPhase = true;
        }
    }
}