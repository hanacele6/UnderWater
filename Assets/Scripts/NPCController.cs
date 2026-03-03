using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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

    [Header("回転演出の設定")]
    [SerializeField] private float rotationSpeed = 5f; // 回転する速さ
    [SerializeField] private float returnDelay = 20f;   // 会話終了後、何秒で元に戻るか

    private Quaternion originalRotation; // 元々の向きを保存
    private Coroutine rotationCoroutine;
    private Transform playerTransform;

    public string GetInteractPrompt()
    {
        return "話しかける";
    }

    void Start()
    {
        // ゲーム開始時の向きを保存しておく
        originalRotation = transform.rotation;
        // プレイヤーのTransformを取得（Playerタグがついている前提）
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void Interact()
    {

        if (DialogueManager.Instance.isTalking)
        {
            DialogueManager.Instance.StartDialogue(null); // 前のロジックを流用してDisplayNextを呼ぶ
            return;
        }

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
            DialogueManager.Instance.StartDialogue(dialogueToPlay, this);
            hasTalkedThisPhase = true;
            StartLookingAtPlayer();
        }
    }

    private void StartLookingAtPlayer()
    {
        if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
        rotationCoroutine = StartCoroutine(LookAtTarget(true));
    }

    // DialogueManagerから「会話が終わった」時に呼んでもらうか、
    // あるいは一定時間監視して戻す
    // 会話終了時に DialogueManager から呼ばれる
    public void StartReturningRotation()
    {
        if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
        // 第二引数に true を渡して、待機してから戻るようにする
        rotationCoroutine = StartCoroutine(LookAtTarget(false, true));
    }

    private IEnumerator LookAtTarget(bool lookAtPlayer, bool waitBeforeReturn = false)
    {
        // ① 元に戻る時だけ、指定された秒数（returnDelay）待機する
        if (waitBeforeReturn)
        {
            yield return new WaitForSeconds(returnDelay);
        }

        float t = 0;
        // 回転開始時の向きを保存（現在の向きからターゲットへ補間するため）
        Quaternion startRot = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;

            Quaternion targetRot;
            if (lookAtPlayer && playerTransform != null)
            {
                Vector3 direction = playerTransform.position - transform.position;
                direction.y = 0; 
                targetRot = Quaternion.LookRotation(direction);
            }
            else
            {
                targetRot = originalRotation;
            }

            // Slerpの第3引数に t を直接使うと線形（一定速度）になるので、
            // より滑らかにするなら t を加工するか、現在の回転から徐々に近づけます
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        // 最後にピタッと合わせる
        if (!lookAtPlayer) transform.rotation = originalRotation;
        
        rotationCoroutine = null;
    }
}