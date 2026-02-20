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
    public int currentDay = 1; // 今何日目か
    public GamePhase currentPhase = GamePhase.Briefing;

    [Header("フラグ管理")]
    // 文字列で管理するフラグ（例: "KeyFound", "TalkedToCaptain"）
    public Dictionary<string, bool> gameFlags = new Dictionary<string, bool>();

    // フェーズが変わった時に通知を送る（UI更新やキャラ移動に使う）
    public UnityEvent<GamePhase> OnPhaseChanged;

    void Awake()
    {
        // シングルトン化（どこからでもアクセスできるようにする）
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // ゲーム開始時の初期化
        StartDay();
    }

    // ==========================================
    // 進行管理システム
    // ==========================================

    // 1日の始まり
    public void StartDay()
    {
        Debug.Log($"=== Day {currentDay} Start ===");
        ChangePhase(GamePhase.Briefing);
    }

    // フェーズを切り替えるメソッド
    public void ChangePhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"フェーズ移行: {currentPhase}");

        // フェーズごとの特殊処理
        switch (currentPhase)
        {
            case GamePhase.Briefing:
                // 朝のBGM再生や、UI表示など
                break;
            case GamePhase.Operation:
                // ソナー操作の解禁など
                break;
            case GamePhase.EventCheck:
                // 自動的にイベント判定を行う
                CheckForEvents();
                break;
            case GamePhase.Incident:
                // 警報音を鳴らすなど
                break;
            case GamePhase.FreeTime:
                // 夜のBGM、ベッドで寝れるようにするなど
                break;
        }

        // 他のスクリプト（UIやキャラ）に「フェーズ変わったよ！」と知らせる
        OnPhaseChanged?.Invoke(currentPhase);
    }

    // 次のフェーズへ進む（ボタンやイベントから呼ぶ）
    public void NextPhase()
    {
        switch (currentPhase)
        {
            case GamePhase.Briefing:
                ChangePhase(GamePhase.Operation);
                break;
            case GamePhase.Operation:
                ChangePhase(GamePhase.EventCheck);
                break;
            case GamePhase.Incident:
                ChangePhase(GamePhase.FreeTime);
                break;
            case GamePhase.FreeTime:
                // 夜が終わったら次の日へ
                GoToNextDay();
                break;
        }
    }

    // ==========================================
    // イベント判定システム (Step 3)
    // ==========================================
    private void CheckForEvents()
    {
        // ここに条件分岐を書く（将来的にはもっと複雑にできます）

        if (GetFlag("ReactorBroken")) 
        {
            // 例：リアクターが壊れているフラグがあれば事件発生！
            Debug.Log("【イベント発生】原子炉の異常検知！");
            ChangePhase(GamePhase.Incident);
        }
        else if (currentDay == 3)
        {
            // 例：3日目は必ず特定のイベントが起きる
            Debug.Log("【イベント発生】謎の通信を傍受");
            ChangePhase(GamePhase.Incident);
        }
        else
        {
            // 何もなければ平和な夜へ（スキップ）
            Debug.Log("今日は特に何も起きなかった...");
            ChangePhase(GamePhase.FreeTime);
        }
    }

    // ==========================================
    // フラグ管理システム
    // ==========================================

    // フラグを立てる（例: SetFlag("FoundEvidence_A", true)）
    public void SetFlag(string flagName, bool value)
    {
        if (gameFlags.ContainsKey(flagName))
        {
            gameFlags[flagName] = value;
        }
        else
        {
            gameFlags.Add(flagName, value);
        }
        Debug.Log($"フラグ更新: {flagName} = {value}");
    }

    // フラグの状態を確認する（例: if(GetFlag("KeyFound")) ...）
    public bool GetFlag(string flagName)
    {
        if (gameFlags.ContainsKey(flagName))
        {
            return gameFlags[flagName];
        }
        return false; // 存在しないフラグはfalseとして扱う
    }

    // 次の日へ移行
    public void GoToNextDay()
    {
        currentDay++;
        // 必要なら日を跨ぐときのリセット処理などをここに書く
        StartDay();
    }
}