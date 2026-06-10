using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using System;
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
    public event Action<int> OnDayChanged;

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

    [Header("完了済みイベント")]
    public List<GameEventData> completedEvents = new List<GameEventData>();
    

    private GameEventData currentPlayingEvent = null; // 現在実行中のイベント

    [Header("カーソル制御")]
    public bool forceShowCursor = false;

    [Header("UI状態")]
    public bool isUIOpen = false;

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

    void Update()
    {

        // 記憶しているフェーズと、現在のInspectorのフェーズが違っていたら手動更新
        if (lastPhase != currentPhase)
        {
            Debug.Log("Inspectorからのフェーズ変更を検知しました！");
            ChangePhase(currentPhase); 
        }
    }

    void LateUpdate()
    {
        if (forceShowCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }



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
        GameEventData morningEvent = CheckForPendingEvents(GamePhase.Briefing, EventTriggerType.AutoOnPhaseStart);

        if (morningEvent != null)
        {
            // 朝イチのイベントがあれば、プレイヤーが動く前にいきなり開始！
            Debug.Log("朝イチのイベントを発見。直ちに開始します。");
            StartEvent(morningEvent);
        }
        else
        {
            // なければ通常通り、プレイヤーが自由に動ける朝としてスタート
            if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeIn(1.0f));
            Debug.Log("今日の朝は特にイベントなし。自由行動開始。");
        }
    }

    public void ChangePhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        lastPhase = currentPhase;
        Debug.Log($"フェーズ移行: {currentPhase}");


        GameEventData pendingEvent = CheckForPendingEvents(currentPhase, EventTriggerType.AutoOnPhaseStart);

        if (pendingEvent != null)
        {
            StartEvent(pendingEvent);
        }
        else
        {
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    public void NextPhase()
    {
        switch (currentPhase)
        {
            case GamePhase.Briefing: ChangePhase(GamePhase.Operation); break;
            case GamePhase.Operation: ChangePhase(GamePhase.FreeTime); break; 
            case GamePhase.FreeTime: GoToNextDay(); break;

            case GamePhase.Incident: ChangePhase(GamePhase.FreeTime); break; 
        }
    }

    // ==========================================
    // 台本（イベント）判定・実行システム
    // ==========================================
    public bool TriggerInteractEvent(string targetID)
    {
        // 今のフェーズで、この targetID を調べた時に起きるイベントがないか探す
        GameEventData pendingEvent = CheckForPendingEvents(currentPhase, EventTriggerType.OnInteract, targetID);

        if (pendingEvent != null)
        {
            StartEvent(pendingEvent);
            return true; // イベントが開始された！
        }
        return false; // 特にイベントはなかった
    }
    private GameEventData CheckForPendingEvents(GamePhase timing, EventTriggerType triggerType, string interactID = "")
    {
        foreach (var eventData in allGameEvents)
        {
            if (completedEvents.Contains(eventData)) continue;

            if (eventData.triggerTiming != timing) continue;
            
            if (eventData.triggerType != triggerType) continue;

            if (triggerType == EventTriggerType.OnInteract && eventData.interactTargetID != interactID) continue;

            if (eventData.requiredDay != 0 && eventData.requiredDay != currentDay) continue;
            if (!string.IsNullOrEmpty(eventData.requiredFlagName) && GetFlag(eventData.requiredFlagName) != eventData.requiredFlagValue) continue;

            return eventData; 
        }
        return null; 
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

        bool isRadio = DialogueManager.Instance != null && DialogueManager.Instance.isRadioMode;

        

        // 1. まず画面を暗転させる
        if (!isRadio && eventData.useScreenFade)
        {
            if (UIManager.Instance != null && UIManager.Instance.fadeCanvasGroup != null) 
            {
                yield return StartCoroutine(UIManager.Instance.FadeOut(0.5f));
            }
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

        if (!eventData.canInteractDuringDialogue)
        {
            LockPlayer();
        }

        // 4. 画面を明るくする
        if (!isRadio && eventData.useScreenFade)
        {
            if (UIManager.Instance != null && UIManager.Instance.fadeCanvasGroup != null) 
            {
                yield return StartCoroutine(UIManager.Instance.FadeIn(0.5f));
            }
        }

        // 5. 会話スタート
        if (eventData.type == EventType.ConversationOnly || 
           (eventData.sentences != null && eventData.sentences.Count > 0))
        {
            DialogueData tempDialogue = ScriptableObject.CreateInstance<DialogueData>();
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

        if (!completedEvents.Contains(currentPlayingEvent))
        {
            completedEvents.Add(currentPlayingEvent);
        }

        if (!currentPlayingEvent.canInteractDuringDialogue)
        {
            UnlockPlayer();
        }

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

    public PlayerInteract playerInteractScript;

    // イベント開始時に呼ぶ

    public void LockPlayer()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (playerCameraScript != null) playerCameraScript.enabled = false;
        if (playerInteractScript != null) playerInteractScript.enabled = false;

        // 物理エンジンをイベント中はずっと眠らせる
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; // 物理演算を停止
        }

 
        forceShowCursor = true; 

        // UI操作のためにカーソルを表示・ロック解除
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // イベント終了時に呼ぶ
    public void UnlockPlayer()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.isRadioMode) return;
        if (isUIOpen) return;
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
        if (playerInteractScript != null) playerInteractScript.enabled = true;

    
        forceShowCursor = false; 

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

        // 現在イベントが再生中でなければ、新しく条件を満たしたイベントがないか探す
        if (currentPlayingEvent == null)
        {
            GameEventData pendingEvent = CheckForPendingEvents(currentPhase, EventTriggerType.AutoOnPhaseStart);
            if (pendingEvent != null)
            {
                Debug.Log($"フラグ '{targetFlagName}' の更新により、イベントが自動発火しました！");
                StartEvent(pendingEvent);
            }
        }
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
                    // 全部ではないが、条件の1つをクリアした時の「進捗通知」
                    UIManager.Instance.ShowMissionNotification($"目的の進捗: {clearedCount}/{mission.targetFlagNames.Count}\n" + mission.displayText);
                    return;
                }
            }

            // ② 新しい目標が「発生」した時の通知
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

        [Header("クリア時報酬フラグ")]
        [Tooltip("この任務を達成した瞬間にONにするフラグ（空欄なら何もしない）")]
        public string setFlagOnClear;

        [Tooltip("このミッションがメニューに表示され始めるフェーズ")]
        public GamePhase appearPhase = GamePhase.Operation; 
        
        [Tooltip("表示される日数（0ならいつでも）")]
        public int appearDay = 0;

        [Tooltip("このフラグがONの時だけメニューに表示する（空欄なら条件なしで表示）")]
        public string requiredFlagToAppear;

        [Header("ガイド設定（複数対応）")]
        [Tooltip("1人称時の目的地（複数の本や端末に対応。空欄ならガイドなし）")]
        public List<Transform> targetLocations = new List<Transform>();

        [Header("ソナー設定（複数対応）")]
        [Tooltip("チェックを入れると、潜水艦のソナー画面にマーカーが出ます")]
        public bool showOnSonar = false;
        
        [Tooltip("ソナーに表示する実際の目的地リスト（複数の岩や沈没船など）")]
        public List<Transform> sonarTargetLocations = new List<Transform>();

        [Header("メインコンソール提出設定")]
        public bool requiresConsoleSubmission = false;
        public List<ItemRequirement> requiredItems = new List<ItemRequirement>();

        [HideInInspector]
        public bool hasNotifiedClear = false;
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

            if (isCleared && !mission.hasNotifiedClear)
            {
                mission.hasNotifiedClear = true; // 2回目以降は出ないようにする
                
                if (SubmarineHUD.Instance != null)
                {
                    SubmarineHUD.Instance.AddLog($"【任務達成】{mission.displayText}", "#FFFF00");
                }

                // 💡 追加：ミッション達成時に任意のフラグを自動発行する
                if (!string.IsNullOrEmpty(mission.setFlagOnClear))
                {
                    SetFlag(mission.setFlagOnClear, true);
                }
            }

            // ② フラグによる出現条件
            bool isAppearFlagSet = string.IsNullOrEmpty(mission.requiredFlagToAppear) || GetFlag(mission.requiredFlagToAppear);

            bool isTimeMet = true;
            if (mission.appearDay > 0)
            {
                if (currentDay < mission.appearDay) isTimeMet = false;
                else if (currentDay == mission.appearDay && currentPhase < mission.appearPhase) isTimeMet = false;
            }
            else
            {
                if (currentPhase < mission.appearPhase) isTimeMet = false;
            }

            // ★すべての条件（未クリア ＋ フラグON ＋ 時間到達）を満たしている場合のみ表示する
            if (!isCleared && isAppearFlagSet && isTimeMet)
            {
                // 💡 複数ソナーターゲットの登録
                if (mission.showOnSonar && mission.sonarTargetLocations != null)
                {
                    foreach (var loc in mission.sonarTargetLocations)
                    {
                        if (loc != null)
                        {
                            activeSonarTargets.Add(new SonarManager.MissionSonarData {
                                target = loc,
                                name = mission.displayText
                            });
                        }
                    }
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
                    
                    if (MissionGuide.Instance != null && mission.targetLocations.Count > 0)
                    {
                        MissionGuide.Instance.SetTargets(mission.targetLocations); 
                    }
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

    [ContextMenu("日数を進めるテスト")]
    public void GoToNextDay()
    {
        currentDay++;
        OnDayChanged?.Invoke(currentDay);
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

    // ==========================================
    // デバッグ用：現在アクティブなミッションのフラグ状況確認
    // ==========================================
    [ContextMenu("🔍 現在のミッションフラグ状況をチェック")]
    public void DebugMissionFlags()
    {
        Debug.Log("<color=cyan>=== 現在発生中（アクティブ）のミッション状況 ===</color>");
        
        int activeCount = 0;

        foreach (var mission in missionList)
        {
            // 1. 出現条件（フラグ）を満たしているか
            bool isAppearFlagSet = string.IsNullOrEmpty(mission.requiredFlagToAppear) || GetFlag(mission.requiredFlagToAppear);

            // 2. 出現条件（時間・フェーズ）を満たしているか
            bool isTimeMet = true;
            if (mission.appearDay > 0)
            {
                if (currentDay < mission.appearDay) isTimeMet = false;
                else if (currentDay == mission.appearDay && currentPhase < mission.appearPhase) isTimeMet = false;
            }
            else
            {
                if (currentPhase < mission.appearPhase) isTimeMet = false;
            }

            // 3. すでにクリア済みかどうかの判定（すべてONならクリア済み）
            bool isCleared = true;
            if (mission.targetFlagNames.Count == 0) 
            {
                isCleared = false; 
            }
            else
            {
                foreach (string flagName in mission.targetFlagNames)
                {
                    if (!GetFlag(flagName)) 
                    { 
                        isCleared = false; 
                        break; 
                    }
                }
            }

            // ★ 出現条件を満たしており、かつ「未クリア」のもの（＝現在アクティブなもの）だけを表示
            if (!isCleared && isAppearFlagSet && isTimeMet)
            {
                activeCount++;
                string log = $"<b>任務: {mission.displayText}</b>\n";

                if (mission.targetFlagNames.Count == 0)
                {
                    log += "  - 設定されているクリアフラグがありません。\n";
                }
                else
                {
                    // 複数のフラグを1つずつチェックして出力
                    foreach (string flag in mission.targetFlagNames)
                    {
                        bool isSet = GetFlag(flag);
                        if (isSet)
                        {
                            log += $"  <color=green>[OK]</color> {flag}\n";
                        }
                        else
                        {
                            log += $"  <color=red>[未達成]</color> {flag}\n";
                        }
                    }
                }
                Debug.Log(log);
            }
        }

        if (activeCount == 0)
        {
            Debug.Log("現在アクティブな（発生中の）ミッションはありません。");
        }
    }

    // ==========================================
    // デバッグ用：イベント台本の発生条件チェック
    // ==========================================
    [ContextMenu("🔍 現在のイベント進行状況をチェック")]
    public void DebugGameEvents()
    {
        Debug.Log("<color=cyan>=== イベント台本の待機・完了状況 ===</color>");

        // 1. 完了済みのイベント一覧
        Debug.Log($"<color=gray>【完了済みのイベント: {completedEvents.Count}件】</color>");
        foreach (var ev in completedEvents)
        {
            if (ev != null)
            {
                Debug.Log($"  <color=gray>- {ev.name} (クリア済)</color>");
            }
        }

        Debug.Log("\n<color=yellow>【未完了（待機中）のイベント】</color>");
        int pendingCount = 0;

        // 2. まだ起きていないイベントの条件をすべてチェック
        foreach (var eventData in allGameEvents)
        {
            // すでに終わったイベントは除外
            if (completedEvents.Contains(eventData)) continue;

            pendingCount++;
            
            // ScriptableObjectのファイル名を表示
            string log = $"<b>台本: {eventData.name}</b>\n";

            // ① 日数チェック
            if (eventData.requiredDay > 0)
            {
                bool isDayMatch = (eventData.requiredDay == currentDay);
                log += isDayMatch ? $"  <color=green>[OK]</color> 日数: Day {eventData.requiredDay}\n" : $"  <color=red>[NG]</color> 日数: Day {eventData.requiredDay} に発生 (現在は Day {currentDay})\n";
            }

            // ② フェーズチェック
            bool isPhaseMatch = (eventData.triggerTiming == currentPhase);
            log += isPhaseMatch ? $"  <color=green>[OK]</color> フェーズ: {eventData.triggerTiming}\n" : $"  <color=red>[NG]</color> フェーズ: {eventData.triggerTiming} のみ (現在は {currentPhase})\n";

            // ③ フラグチェック
            if (!string.IsNullOrEmpty(eventData.requiredFlagName))
            {
                bool currentFlagStatus = GetFlag(eventData.requiredFlagName);
                bool isFlagMatch = (currentFlagStatus == eventData.requiredFlagValue);
                string expected = eventData.requiredFlagValue ? "ON" : "OFF";
                string actual = currentFlagStatus ? "ON" : "OFF";

                log += isFlagMatch ? $"  <color=green>[OK]</color> 必須フラグ: '{eventData.requiredFlagName}' が {expected}\n" : $"  <color=red>[NG]</color> 必須フラグ: '{eventData.requiredFlagName}' が {expected} であること (現在は {actual})\n";
            }

            // ④ トリガーの種類（どうやって発生するか）
            log += $"  <color=white>[INFO]</color> トリガー: {eventData.triggerType}\n";
            if (eventData.triggerType == EventTriggerType.OnInteract) // ユーザーが調べるタイプの場合
            {
                 log += $"    ┗ 対象ID: '{eventData.interactTargetID}' を調べた時\n";
            }

            Debug.Log(log);
        }

        if (pendingCount == 0)
        {
            Debug.Log("未完了のイベントはありません。");
        }
    }
}