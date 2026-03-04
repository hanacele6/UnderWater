using UnityEngine;

public class FlagSetter : MonoBehaviour, IInteractable
{
    [Header("インタラクト設定")]
    public string promptText = "調べる";
    
    [Header("フラグ設定 (調べた時の結果)")]
    [Tooltip("立てたいフラグの名前を入力してください（空欄なら何もしない）")]
    public string targetFlagName;
    [Tooltip("調べた時にフラグをTrueにするか、Falseにするか")]
    public bool setFlagTo = true;

    [Header("イベント出現・進行 (オプション)")]
    [Tooltip("【出現条件】このフラグがONの時だけ出現する（空欄なら常に表示）")]
    public string requiredFlagToAppear;
    
    [Tooltip("【イベント進行】調べた時に、現在進行中の割り込みイベント(台本)を完了させるか？")]
    public bool completesEvent = false;

    [Header("クリア後の処理")]
    [Tooltip("1回調べたら、もう調べられないようにするか？")]
    public bool disableAfterInteract = true;
    
    [Tooltip("調べた後、オブジェクト自体を非表示（消去）にするか？")]
    public bool hideAfterInteract = false;

    private bool hasInteracted = false;

    void Start()
    {
        // GameManagerが存在すれば、フェーズが変わるたびに出現チェックを行う
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged.AddListener(CheckVisibility);
            // ゲーム開始時にも1回チェックする
            CheckVisibility(GameManager.Instance.currentPhase);
        }
    }

    // フェーズ変更時に呼ばれる：自分を表示すべきか隠すべきか判定
    private void CheckVisibility(GamePhase phase)
    {
        // 出現条件の指定がなければ、常に表示のまま何もしない
        if (string.IsNullOrEmpty(requiredFlagToAppear)) return;

        // 指定されたフラグの状態を取得して、アクティブ状態を切り替える
        bool shouldAppear = GameManager.Instance.GetFlag(requiredFlagToAppear);
        
        // ※一度調べ終わって隠れた状態（hideAfterInteract）なら再表示しない
        if (hasInteracted && hideAfterInteract) return;

        gameObject.SetActive(shouldAppear);
    }

    public string GetInteractPrompt()
    {
        if (hasInteracted) return ""; // 既に調べた後ならテキストを出さない
        return promptText;
    }

    public void Interact()
    {
        if (hasInteracted) return;

        // 1. フラグの更新
        if (GameManager.Instance != null && !string.IsNullOrEmpty(targetFlagName))
        {
            GameManager.Instance.SetFlag(targetFlagName, setFlagTo);
        }

        // 2. 台本イベントの完了（探索イベントのクリア！）
        if (completesEvent && GameManager.Instance != null)
        {
            GameManager.Instance.CompleteCurrentEvent();
        }

        // 3. インタラクト後の状態変化
        if (disableAfterInteract)
        {
            hasInteracted = true;
        }

        if (hideAfterInteract)
        {
            gameObject.SetActive(false);
        }
    }
}