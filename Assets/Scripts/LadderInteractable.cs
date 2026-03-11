using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LadderInteractable : MonoBehaviour, IInteractable
{
    [Header("ワープ先の設定")]
    [Tooltip("下にいる時に「昇る」を選んだ場合のワープ先")]
    public Transform topDestination;
    
    [Tooltip("上にいる時に「降りる」を選んだ場合のワープ先")]
    public Transform bottomDestination;

    void Start()
    {
        // 物理的にぶつからないようにする
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    // ==========================================
    // プレイヤーが上と下のどちらにいるかを判定する便利メソッド
    // ==========================================
    private bool IsPlayerAtBottom(GameObject player)
    {
        if (topDestination == null || bottomDestination == null) return true;

        // ワープ先（上）とワープ先（下）のちょうど真ん中の高さを計算する
        float midY = (topDestination.position.y + bottomDestination.position.y) / 2f;
        
        // プレイヤーの高さ（Y座標）が真ん中より低ければ「下にいる」と判定
        return player.transform.position.y < midY;
    }

    // ==========================================
    // IInteractable の実装
    // ==========================================
    public string GetInteractPrompt()
    {
        if (SubmarineController.Instance != null && SubmarineController.Instance.isPiloting) return "";

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return "";

        // 自分が下にいるなら「昇る」、上にいるなら「降りる」を返す
        return IsPlayerAtBottom(player) ? "昇る" : "降りる";
    }

    public void Interact()
    {
        if (SubmarineController.Instance != null && SubmarineController.Instance.isPiloting) return;

        if (topDestination == null || bottomDestination == null)
        {
            Debug.LogError($"{gameObject.name} に目的地が設定されていません！");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // 今いる位置に応じて、目標のワープ先を決める
        Transform targetDest = IsPlayerAtBottom(player) ? topDestination : bottomDestination;

        TeleportPlayer(player, targetDest);
    }

    private void TeleportPlayer(GameObject player, Transform dest)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 位置をワープ
        player.transform.position = dest.position;
        // 向きもワープ先（Destination）の向きに合わせる
        player.transform.rotation = dest.rotation;

        if (cc != null) cc.enabled = true;
    }
}