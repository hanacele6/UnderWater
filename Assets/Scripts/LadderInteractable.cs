using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LadderInteractable : MonoBehaviour, IInteractable
{
    [Header("ワープ先の設定")]
    [Tooltip("下にいる時に「昇る」を選んだ場合のワープ先")]
    public Transform topDestination;
    
    [Tooltip("上にいる時に「降りる」を選んだ場合のワープ先")]
    public Transform bottomDestination;

    // ==========================================
    // 💡 追加：アクセス制限機能とフラグ送信機能
    // ==========================================
    [Header("【アクセス制限】")]
    [Tooltip("このフラグがONの時だけはしごを使えます（空欄ならいつでも可）")]
    public string requiredFlagToInteract;
    
    [Tooltip("ロックされている時に画面に出る文字")]
    public string lockedPromptMessage = "今はまだ昇る必要はなさそうだ";

    [Header("【フラグ操作】")]
    [Tooltip("はしごを使った瞬間にONにしたいフラグ名（空欄なら何もしない）")]
    public string setFlagOnInteract;
    // ==========================================

    void Start()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private bool IsPlayerAtBottom(GameObject player)
    {
        if (topDestination == null || bottomDestination == null) return true;

        float midY = (topDestination.position.y + bottomDestination.position.y) / 2f;
        return player.transform.position.y < midY;
    }

    // 💡 ロック状態かどうかを判定する
    private bool IsLocked()
    {
        if (string.IsNullOrEmpty(requiredFlagToInteract)) return false; 
        if (GameManager.Instance == null) return false;
        return !GameManager.Instance.GetFlag(requiredFlagToInteract); 
    }

    public string GetInteractPrompt()
    {
        if (SubmarineController.Instance != null && SubmarineController.Instance.isPiloting) return "";

        // 💡 ロックされている時の表示
        if (IsLocked()) return lockedPromptMessage;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return "";

        return IsPlayerAtBottom(player) ? "昇る" : "降りる";
    }

    public void Interact()
    {
        if (SubmarineController.Instance != null && SubmarineController.Instance.isPiloting) return;

        // 💡 ロックされている時はワープさせない
        if (IsLocked())
        {
            UIManager.Instance.ShowMessage("上の階へ行く必要はまだなさそうだ。");
            return;
        }

        if (topDestination == null || bottomDestination == null)
        {
            Debug.LogError($"{gameObject.name} に目的地が設定されていません！");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // 💡 ワープ成功時にフラグを送信する
        if (!string.IsNullOrEmpty(setFlagOnInteract) && GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag(setFlagOnInteract, true);
        }

        Transform targetDest = IsPlayerAtBottom(player) ? topDestination : bottomDestination;
        TeleportPlayer(player, targetDest);
    }

    private void TeleportPlayer(GameObject player, Transform dest)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = dest.position;
        player.transform.rotation = dest.rotation;

        if (cc != null) cc.enabled = true;
    }
}