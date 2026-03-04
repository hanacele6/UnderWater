using UnityEngine;
using System.Collections.Generic;

// ==========================================
// イベントの種類を定義
// ==========================================
public enum EventType
{
    ConversationOnly, // 画面固定の会話のみ（プレイヤー操作ロック）
    PlayableIncident  // 証拠品を探すなどの操作可能フェーズ（Incidentフェーズへ移行）
}

// ==========================================
// 会話データの構造体
// ==========================================
[System.Serializable]
public struct DialogueLine
{
    [Tooltip("話者名（例：艦長、システム音声、主人公など）")]
    public string speakerName;
    
    [TextArea(2, 4)]
    [Tooltip("セリフ内容")]
    public string message;
}

// ==========================================
// ゲームの台本（イベントデータ）本体
// ==========================================
[CreateAssetMenu(fileName = "NewGameEvent", menuName = "Submarine/GameEventData")]
public class GameEventData : ScriptableObject
{
    [Header("発生条件")]
    [Tooltip("このイベントが発生する日付（0ならいつでも）")]
    public int requiredDay = 0;
    
    [Tooltip("どのフェーズの『直後』に発生するか")]
    public GamePhase triggerTiming;

    [Tooltip("必要なフラグ（空ならフラグ不要）")]
    public string requiredFlagName;
    
    [Tooltip("trueならフラグONで発生、falseならOFFで発生")]
    public bool requiredFlagValue = true; 

    [Header("イベント基本設定")]
    public EventType type;
    
    [Tooltip("管理用のメモ（例：「1日目：ソナー異常の発見」など）")]
    public string eventMemo; 

    [Tooltip("このイベントが始まった瞬間にONにするフラグ（証拠品の出現用など）")]
    public string startEventFlag; 

    [Header("会話データ (ConversationOnly用)")]
    [Tooltip("上から順番にテキストが表示されます。PlayableIncidentの場合は空でOKです。")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("終了後の動作")]
    [Tooltip("このイベント（会話終了 or 証拠品発見）が終わった後、どのフェーズに移行するか")]
    public GamePhase nextPhaseAfterEvent;
    
    [Tooltip("イベントクリア後に立てるフラグ（空欄なら何もしない）")]
    public string setFlagOnComplete;
}