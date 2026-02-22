using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

// 1日のフェーズ（状態）を定義
public enum GamePhase
{
    Briefing,   // 1. 目標確認・朝の会話
    Operation,  // 2. 潜水艦活動（ソナー・探索）
    EventCheck, // 3. イベント判定（自動分岐）
    Incident,   // 4. 事件・イベントパート
    FreeTime    // 5. 夜・自由行動
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("現在の状況")]
    public int currentDay = 1;
    public GamePhase currentPhase = GamePhase.Briefing;
    private GamePhase lastPhase; // インスペクター変更検知用

    [Header("フラグ管理")]
    public Dictionary<string, bool> gameFlags = new Dictionary<string, bool>();
    public UnityEvent<GamePhase> OnPhaseChanged;

    // ==========================================
    // データ駆動：フェーズ移行ルールの設定
    // ==========================================
    [System.Serializable]
    public struct PhaseTransitionRule
    {
        public string memo;              // メモ（例：ソナー起動で探索へ）
        public GameObject targetObject;  // 触る対象のオブジェクト（Hierarchyからドラッグ＆ドロップ）
        public GamePhase requiredPhase;  // 条件：このフェーズの時だけ発動
        public GamePhase nextPhase;      // 結果：このフェーズに移行する
    }

    [Header("フェーズ移行ルール設定")]
    [Tooltip("どのオブジェクトを触ったら、どのフェーズに進むかを設定します")]
    public List<PhaseTransitionRule> transitionRules = new List<PhaseTransitionRule>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        lastPhase = currentPhase;
        StartDay();
    }

#if UNITY_EDITOR
    void Update()
    {
        // 記憶しているフェーズと、現在のInspectorのフェーズが違っていたら手動更新
        if (lastPhase != currentPhase)
        {
            Debug.Log("Inspectorからのフェーズ変更を検知しました！");
            ChangePhase(currentPhase); 
        }
    }
#endif

    // ==========================================
    // オブジェクトを調べた時のフェーズ移行チェック
    // ==========================================
    public void CheckPhaseTransition(GameObject interactedObject)
    {
        // インスペクターで設定したルールを上から順に確認
        foreach (var rule in transitionRules)
        {
            bool isMatch = (rule.targetObject == interactedObject) || 
                           (interactedObject.transform.IsChildOf(rule.targetObject.transform));

            // 触ったオブジェクトと、条件のフェーズが一致したらフェーズを変える
            if (isMatch && currentPhase == rule.requiredPhase)
            {
                Debug.Log($"ルール【{rule.memo}】が発動: {currentPhase} -> {rule.nextPhase}");
                ChangePhase(rule.nextPhase);
                return; // 1回変わったら終わる（重複発動防止）
            }
        }
    }

    // ==========================================
    // 進行管理システム本体
    // ==========================================
    public void StartDay()
    {
        Debug.Log($"=== Day {currentDay} Start ===");
        ChangePhase(GamePhase.Briefing);
    }

    public void ChangePhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        lastPhase = currentPhase;
        Debug.Log($"フェーズ移行: {currentPhase}");

        switch (currentPhase)
        {
            case GamePhase.Briefing: break;
            case GamePhase.Operation: break;
            case GamePhase.EventCheck:
                CheckForEvents(); // イベント判定を自動実行
                break;
            case GamePhase.Incident: break;
            case GamePhase.FreeTime: break;
        }

        // UIやNPCに「フェーズが変わった！」と通知する
        OnPhaseChanged?.Invoke(currentPhase);
    }

    public void NextPhase()
    {
        switch (currentPhase)
        {
            case GamePhase.Briefing: ChangePhase(GamePhase.Operation); break;
            case GamePhase.Operation: ChangePhase(GamePhase.EventCheck); break;
            case GamePhase.Incident: ChangePhase(GamePhase.FreeTime); break;
            case GamePhase.FreeTime: GoToNextDay(); break;
        }
    }

    // ==========================================
    // イベント自動判定システム
    // ==========================================
    private void CheckForEvents()
    {
        if (GetFlag("ReactorBroken")) 
        {
            Debug.Log("【イベント発生】原子炉の異常検知！");
            ChangePhase(GamePhase.Incident);
        }
        else
        {
            Debug.Log("今日は特に異常なし。");
            ChangePhase(GamePhase.FreeTime);
        }
    }

    // ==========================================
    // フラグ管理システム（インスペクター対応版）
    // ==========================================
    [System.Serializable]
    public class EventFlag
    {
        public string flagName; 
        public bool isTrue;     
    }

    [Header("現在のフラグ一覧（実行中のみ確認・編集可）")]
    public List<EventFlag> activeFlags = new List<EventFlag>();

    public void SetFlag(string targetFlagName, bool value)
    {
        EventFlag existingFlag = activeFlags.Find(f => f.flagName == targetFlagName);

        if (existingFlag != null)
        {
            existingFlag.isTrue = value; 
        }
        else
        {
            activeFlags.Add(new EventFlag { flagName = targetFlagName, isTrue = value });
        }
        
        Debug.Log($"フラグ更新: {targetFlagName} = {value}");

        // フラグが更新されたら、通知を出すかチェックする
        CheckMissionNotification(targetFlagName);
    }

    public bool GetFlag(string targetFlagName)
    {
        EventFlag existingFlag = activeFlags.Find(f => f.flagName == targetFlagName);
        if (existingFlag != null) return existingFlag.isTrue;
        return false; 
    }

    // ==========================================
    // 通知判定メソッド（ここでフラグとミッションを照らし合わせる）
    // ==========================================
    private void CheckMissionNotification(string updatedFlag)
    {
        if (UIManager.Instance == null) return;

        foreach (var mission in missionList)
        {
            // そのフラグがミッションの「達成条件」だった場合
            if (mission.targetFlagName == updatedFlag && GetFlag(updatedFlag) == true)
            {
                UIManager.Instance.ShowMissionNotification("目的を達成しました\n" + mission.displayText);
                return;
            }
        }
    }

    // ==========================================
    // ミッション（目的）管理システム
    // ==========================================
    [System.Serializable]
    public class MissionObjective
    {
        public string memo;              
        public string displayText;       

        [TextArea(3, 5)]
        public string description;
        public bool isMainObjective;     
        
        [Tooltip("このフラグがTrueになったら『達成済み』になる")]
        public string targetFlagName;    

        [Tooltip("このミッションがメニューに表示され始めるフェーズ")]
        public GamePhase appearPhase = GamePhase.Operation; 
        
        [Tooltip("表示される日数（0ならいつでも）")]
        public int appearDay = 0;
    }

    [Header("現在のミッション一覧")]
    public List<MissionObjective> missionList = new List<MissionObjective>();


    public void GoToNextDay()
    {
        currentDay++;
        StartDay();
    }
}