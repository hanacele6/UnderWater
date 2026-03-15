using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

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

    [Header("イベント台本（Projectから登録）")]
    [Tooltip("発生しうるすべてのイベントデータをここに登録します")]
    public List<GameEventData> allGameEvents = new List<GameEventData>();

    private GameEventData currentPlayingEvent = null; // 現在実行中のイベント

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
        StartCoroutine(StartDayRoutine()); // コルーチンとして呼び出すように変更
    }

    private IEnumerator StartDayRoutine()
    {
        Debug.Log($"=== Day {currentDay} Start ===");

        // 1. まずフェーズをその日の始まり（Briefing）にセットする
        currentPhase = GamePhase.Briefing;
        lastPhase = currentPhase;
        OnPhaseChanged?.Invoke(currentPhase);
        UpdateMainMissionHUD();

        // 2. 朝イチ（Briefing開始直後）に発生すべき台本がないかチェックする
        GameEventData morningEvent = CheckForPendingEvents(GamePhase.Briefing);

        if (morningEvent != null)
        {
            // 朝イチのイベントがあれば、プレイヤーが動く前にいきなり開始！
            Debug.Log("朝イチのイベントを発見。直ちに開始します。");
            StartEvent(morningEvent);
        }
        else
        {
            // なければ通常通り、プレイヤーが自由に動ける朝としてスタート
            // ★追加：イベントがない平和な朝なら、ただ画面を明るくしてゲーム開始
            if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeIn(1.0f));
            Debug.Log("今日の朝は特にイベントなし。自由行動開始。");
        }
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

    // ==========================================
    // 進行管理システム本体（NextPhaseを改修）
    // ==========================================
    public void NextPhase()
    {
        // 次の日常フェーズに進む前に、割り込むべきイベント（台本）がないかチェックする
        GameEventData pendingEvent = CheckForPendingEvents(currentPhase);

        if (pendingEvent != null)
        {
            // 割り込みイベント発生！
            StartEvent(pendingEvent);
        }
        else
        {
            // イベントがなければ通常の1日の流れを進める
            switch (currentPhase)
            {
                case GamePhase.Briefing: ChangePhase(GamePhase.Operation); break;
                case GamePhase.Operation: ChangePhase(GamePhase.FreeTime); break; // EventCheckを廃止し直接遷移
                case GamePhase.FreeTime: GoToNextDay(); break;
                
                // イベント終了時などはここ呼ばれる想定
                case GamePhase.Incident: ChangePhase(GamePhase.FreeTime); break; 
            }
        }
    }

    // ==========================================
    // 台本（イベント）判定・実行システム
    // ==========================================
    private GameEventData CheckForPendingEvents(GamePhase timing)
    {
        foreach (var eventData in allGameEvents)
        {
            // 条件1: フェーズのタイミングが一致しているか
            if (eventData.triggerTiming != timing) continue;

            // 条件2: 日数指定がある場合、一致しているか
            if (eventData.requiredDay != 0 && eventData.requiredDay != currentDay) continue;

            // 条件3: フラグ条件を満たしているか
            if (!string.IsNullOrEmpty(eventData.requiredFlagName))
            {
                if (GetFlag(eventData.requiredFlagName) != eventData.requiredFlagValue) continue;
            }

            return eventData; // 全条件クリア！このイベントを再生する
        }
        return null; // 発生するイベントなし
    }

    // GameManager.cs 内
    // ==========================================
    // イベント実行
    // ==========================================
    private void StartEvent(GameEventData eventData)
    {
        StartCoroutine(EventSequence(eventData)); // コルーチンとして呼び出すように変更
    }

    private IEnumerator EventSequence(GameEventData eventData)
    {
        currentPlayingEvent = eventData;

        // 1. まず画面を暗転させる
        if (UIManager.Instance != null && UIManager.Instance.fadeCanvasGroup != null) 
        {
            yield return StartCoroutine(UIManager.Instance.FadeOut(0.5f));
        }

        // 2. フラグ立て
        if (!string.IsNullOrEmpty(eventData.startEventFlag))
        {
            SetFlag(eventData.startEventFlag, true);
        }

        // 3. 舞台セットアップ（ここでワープを完了させる！）
        if (!string.IsNullOrEmpty(eventData.targetStageID))
        {
            EventStage[] stages = FindObjectsOfType<EventStage>();
            foreach (var stage in stages)
            {
                if (stage.stageID == eventData.targetStageID)
                {
                    currentStage = stage;
                    currentStage.SetupStage();
                    break;
                }
            }
        }

        yield return null;

        LockPlayer();

        // 4. 画面を明るくする
        if (UIManager.Instance != null && UIManager.Instance.fadeCanvasGroup != null) 
        {
            yield return StartCoroutine(UIManager.Instance.FadeIn(0.5f));
        }

        // 5. 会話スタート
        if (eventData.type == EventType.ConversationOnly || 
           (eventData.sentences != null && eventData.sentences.Count > 0))
        {
            DialogueData tempDialogue = ScriptableObject.CreateInstance<DialogueData>();
            tempDialogue.sentences = eventData.sentences;

            tempDialogue.sentences = eventData.sentences;
            
            DialogueManager.Instance.StartDialogue(tempDialogue, CompleteCurrentEvent);
        }
        else if (eventData.type == EventType.PlayableIncident)
        {
            ChangePhase(GamePhase.Incident);
            UnlockPlayer();
        }
    }

    private EventStage currentStage; // 現在使っている舞台
    // イベント（会話や探索）が終わった時に外部から呼ばれるメソッド
    public void CompleteCurrentEvent()
    {
        if (currentPlayingEvent == null) return;

        // 舞台の片付け（NPCを消すなど）
        if (currentStage != null)
        {
            currentStage.CleanupStage();
            currentStage = null;
        }

        UnlockPlayer();

        Debug.Log($"【イベント完了】: {currentPlayingEvent.eventMemo}");

        // イベントクリア報酬のフラグを立てる
        if (!string.IsNullOrEmpty(currentPlayingEvent.setFlagOnComplete))
        {
            SetFlag(currentPlayingEvent.setFlagOnComplete, true);
        }

        GamePhase next = currentPlayingEvent.nextPhaseAfterEvent;
        currentPlayingEvent = null; // リセット

        // 台本で指定された次のフェーズへ移行
        ChangePhase(next);
    }

    [Header("プレイヤー制御")]
    public MonoBehaviour playerMovementScript; // プレイヤーの移動スクリプト
    public MonoBehaviour playerCameraScript;   // 視点移動（MouseLookなど）のスクリプト

    // イベント開始時に呼ぶ

    public void LockPlayer()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (playerCameraScript != null) playerCameraScript.enabled = false;

        // 物理エンジンをイベント中はずっと眠らせる
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; // 物理演算を停止
        }

        // UI操作のためにカーソルを表示・ロック解除
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // イベント終了時に呼ぶ
    public void UnlockPlayer()
    {
        // 物理エンジンを目覚めさせる
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false; // 物理演算を再開
        }

        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (playerCameraScript != null) playerCameraScript.enabled = true;

        // 再びゲーム用にカーソルを隠す・ロック
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
            // ==========================================
            // すでに同じ状態（ONなのにONにしようとした等）なら、通知を出さずに即終了！
            // ==========================================
            if (existingFlag.isTrue == value) return;

            existingFlag.isTrue = value; 
        }
        else
        {
            activeFlags.Add(new EventFlag { flagName = targetFlagName, isTrue = value });
        }
        
        Debug.Log($"フラグ更新: {targetFlagName} = {value}");

        // フラグが「新しく」更新された時だけ、通知とHUDのチェックを行う
        CheckMissionNotification(targetFlagName);
        UpdateMainMissionHUD();
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
            // ① 目標を「達成」した時の通知
            // ★変更：更新されたフラグが、このミッションの条件リストに含まれているか？
            if (mission.targetFlagNames.Contains(updatedFlag) && GetFlag(updatedFlag) == true)
            {
                // リストに含まれていた場合、ミッションの「全て」のフラグがONになったか確認する
                bool isFullyCleared = true;
                int clearedCount = 0; // （おまけ）進捗表示用

                foreach (string flag in mission.targetFlagNames)
                {
                    if (!GetFlag(flag))
                    {
                        isFullyCleared = false;
                    }
                    else
                    {
                        clearedCount++;
                    }
                }

                if (isFullyCleared)
                {
                    // 全部ONなら、完全クリアの通知！
                    UIManager.Instance.ShowMissionNotification("目的を達成しました\n" + mission.displayText);
                    return; 
                }
                else
                {
                    // 全部ではないが、条件の1つをクリアした時の「進捗通知」（お好みで！）
                    UIManager.Instance.ShowMissionNotification($"目的の進捗: {clearedCount}/{mission.targetFlagNames.Count}\n" + mission.displayText);
                    return;
                }
            }

            // ② 新しい目標が「発生」した時の通知（※ここは元のままでOKです！）
            if (mission.requiredFlagToAppear == updatedFlag && GetFlag(updatedFlag) == true)
            {
                UIManager.Instance.ShowMissionNotification("新しい目的が追加されました");
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
        
        [Tooltip("クリアに必要なフラグのリスト（全てONでクリア）")]
        public List<string> targetFlagNames = new List<string>();

        [Tooltip("このミッションがメニューに表示され始めるフェーズ")]
        public GamePhase appearPhase = GamePhase.Operation; 
        
        [Tooltip("表示される日数（0ならいつでも）")]
        public int appearDay = 0;

        [Tooltip("このフラグがONの時だけメニューに表示する（空欄なら条件なしで表示）")]
        public string requiredFlagToAppear;

        [Header("ガイド設定（1人称視点用）")]
        [Tooltip("1人称時の目的地（操舵輪やドアなど。空欄ならガイドなし）")]
        public Transform targetLocation;

        [Header("ソナー設定（潜水艦視点用）")]
        [Tooltip("チェックを入れると、潜水艦のソナー画面にマーカーが出ます")]
        public bool showOnSonar = false;
        
        [Tooltip("ソナーに表示する実際の目的地（岩や沈没船など）")]
        public Transform sonarTargetLocation;
    }

    [Header("現在のミッション一覧")]
    public List<MissionObjective> missionList = new List<MissionObjective>();

    


    // ==========================================
    // HUD（常時表示パネル）の更新処理
    // ==========================================
    public void UpdateMainMissionHUD()
    {
        if (UIManager.Instance == null) return;

        List<SonarManager.MissionSonarData> activeSonarTargets = new List<SonarManager.MissionSonarData>(); 
        string mainQuestText = "";
        
        string missionListDisplay = "<color=#88FF88>【アクティブな任務】</color>\n";
        int activeCount = 0;

        foreach (var mission in missionList)
        {
            // ① クリア済みかどうかの判定
            bool isCleared = true;
            foreach (string flagName in mission.targetFlagNames)
            {
                if (!GetFlag(flagName)) { isCleared = false; break; }
            }
            if (mission.targetFlagNames.Count == 0) isCleared = false;

            // ② フラグによる出現条件
            bool isAppearFlagSet = string.IsNullOrEmpty(mission.requiredFlagToAppear) || GetFlag(mission.requiredFlagToAppear);

            // ==========================================
            // ③ ★追加：日数とフェーズによる出現条件をチェック！
            // ==========================================
            bool isTimeMet = true;
            if (mission.appearDay > 0)
            {
                // まだその日になっていない場合
                if (currentDay < mission.appearDay) isTimeMet = false;
                // 日数は同じだが、まだ指定されたフェーズになっていない場合
                else if (currentDay == mission.appearDay && currentPhase < mission.appearPhase) isTimeMet = false;
            }
            else
            {
                // 日数指定が0（いつでも）でも、フェーズの指定は守る
                if (currentPhase < mission.appearPhase) isTimeMet = false;
            }

            // ★すべての条件（未クリア ＋ フラグON ＋ 時間到達）を満たしている場合のみ表示する
            if (!isCleared && isAppearFlagSet && isTimeMet)
            {
                if (mission.showOnSonar && mission.sonarTargetLocation != null)
                {
                    activeSonarTargets.Add(new SonarManager.MissionSonarData {
                        target = mission.sonarTargetLocation,
                        name = mission.displayText
                    });
                }

                if (mission.showOnSonar)
                {
                    string typeLabel = mission.isMainObjective ? "[MAIN]" : "[SUB]";
                    missionListDisplay += $"{typeLabel} {mission.displayText}\n";
                    activeCount++;
                }

                if (mission.isMainObjective && string.IsNullOrEmpty(mainQuestText))
                {
                    mainQuestText = mission.displayText;
                    if (MissionGuide.Instance != null) MissionGuide.Instance.SetTarget(mission.targetLocation);
                }
            }
        }

        if (activeCount == 0) missionListDisplay += "現在、指示されている任務はありません。";

        UIManager.Instance.UpdateMainMission(mainQuestText); 
        
        if (SonarManager.Instance != null)
            SonarManager.Instance.SetMissionTargets(activeSonarTargets);

        if (SubmarineHUD.Instance != null)
            SubmarineHUD.Instance.UpdateMissionListText(missionListDisplay);
    }

    public void GoToNextDay()
    {
        currentDay++;
        StartDay();
    }

    public void UpdateCameraTarget(string speakerName)
    {
        // 現在舞台がセットアップされていれば、喋っている人の方を向かせる
        if (currentStage != null)
        {
            currentStage.LookAtSpeaker(speakerName);
        }
    }
}