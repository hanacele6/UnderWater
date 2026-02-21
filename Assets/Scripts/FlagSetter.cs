using UnityEngine;

public class FlagSetter : MonoBehaviour, IInteractable
{
    [Header("インタラクト設定")]
    public string promptText = "調べる";
    
    [Header("フラグ設定")]
    [Tooltip("立てたいフラグの名前を入力してください（例：FoundEvidence）")]
    public string targetFlagName;
    
    [Tooltip("調べた時にフラグをTrueにするか、Falseにするか")]
    public bool setFlagTo = true;

    [Tooltip("1回調べたら、もう調べられないようにするか？")]
    public bool disableAfterInteract = true;

    private bool hasInteracted = false;

    public string GetInteractPrompt()
    {
        if (hasInteracted) return ""; // 既に調べた後ならテキストを出さない
        return promptText;
    }

    public void Interact()
    {
        if (hasInteracted) return;

        // GameManagerにフラグの更新を命令！
        if (GameManager.Instance != null && !string.IsNullOrEmpty(targetFlagName))
        {
            GameManager.Instance.SetFlag(targetFlagName, setFlagTo);
        }

        if (disableAfterInteract)
        {
            hasInteracted = true;
            // 必要ならここで「書類のオブジェクトを消す」などの処理を追加してもOK
            // gameObject.SetActive(false); 
        }
    }
}