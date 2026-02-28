using UnityEngine;

// 以前作成した IInteractable インターフェースを継承
[RequireComponent(typeof(Collider))] // 当たり判定が必要
public class LadderInteractable : MonoBehaviour, IInteractable
{
    public enum LadderType
    {
        BottomToTop, // 下に置いてあるトリガー（昇る用）
        TopToBottom  // 上に置いてあるトリガー（降りる用）
    }

    [Header("はしごの設定")]
    public LadderType type = LadderType.BottomToTop;
    
    [Tooltip("ワープ先の場所（もう一方のトリガー付近に置いたTransformを指定）")]
    public Transform destination; 

    [Tooltip("移動後にプレイヤーが向く方向（空欄ならプレイヤーの向きを維持）")]
    public Transform lookAtTarget;

    void Start()
    {
        // Colliderの設定を自動でTriggerにする（物理的にぶつからないようにする）
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    // ==========================================
    // IInteractable の実装
    // ==========================================

    public string GetInteractPrompt()
    {
        // 潜水艦が操縦中（ソナーON）の時は、はしごを使えないようにする
        if (SubmarineController.Instance != null && SubmarineController.Instance.isPiloting)
        {
            return ""; 
        }

        // 自分が上か下かによってプロンプトを変える
        return type == LadderType.BottomToTop ? "昇る" : "降りる";
    }

    public void Interact()
    {
        // 操縦中はインタラクトしても無視
        if (SubmarineController.Instance != null && SubmarineController.Instance.isPiloting) return;

        if (destination == null)
        {
            Debug.LogError($"{gameObject.name} に目的地(Destination)が設定されていません！");
            return;
        }

        // シーン内のプレイヤー（FindObjectOfTypeは重いので、本番はManagerから取得推奨）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // ★ワープ処理：プレイヤーの位置と向きを、目的地のTransformに瞬時に書き換える！
        TeleportPlayer(player);
    }

    private void TeleportPlayer(GameObject player)
    {
        // CharacterControllerがONだと移動が反映されないことがあるので、一時的にオフにする
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 位置をワープ
        player.transform.position = destination.position;

        // 向きも合わせる（指定があれば）
        if (lookAtTarget != null)
        {
            Vector3 lookPos = lookAtTarget.position;
            lookPos.y = player.transform.position.y; // 上下は向かせない
            player.transform.LookAt(lookPos);
        }
        else
        {
            // 指定がなければ目的地のRotationに合わせる
            player.transform.rotation = destination.rotation;
        }

        // CharacterControllerを元に戻す
        if (cc != null) cc.enabled = true;

        // 必要であれば、ここに「足音のSE」などを入れると自然になります
    }
}