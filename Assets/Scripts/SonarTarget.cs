using UnityEngine;

public enum SubmarineTargetType
{
    Mine,           // 機雷
    HostileBio,     // 敵性生物
    NeutralBio,     // 中立生物
    Item            // アイテム
}

public class SonarTarget : MonoBehaviour
{
    [Header("Target Settings")]
    public SubmarineTargetType targetType = SubmarineTargetType.Mine;

    [Header("Damage Settings")]
    [Tooltip("機雷だった場合、プレイヤーに与えるダメージ量")]
    public float damageAmount = 25f;

    [Header("Item Settings (アイテムの場合のみ)")]
    [Tooltip("特定アイテムなら指定。空欄（None）ならランダムドロップになります")]
    public ItemData specificItemData;

    private void OnTriggerEnter(Collider other)
    {
        SubmarineStatus sub = other.GetComponentInParent<SubmarineStatus>();
        if (sub == null) return;

        // 1. 機雷または敵対生物のダメージ処理
        if (targetType == SubmarineTargetType.Mine || targetType == SubmarineTargetType.HostileBio)
        {
            // プレイヤーのHPを減らす
            sub.TakeDamage(damageAmount);
            
            string attackName = targetType == SubmarineTargetType.Mine ? "機雷の爆発" : "巨大生物の激突";
            Debug.Log($"{attackName}！ ダメージ: {damageAmount} / 残りHP: {sub.currentHP}");
            
            // ターゲットを海から消し去る
            Destroy(gameObject);
        }

        // 2. アイテムの回収処理
        else if (targetType == SubmarineTargetType.Item)
        {
            // キュー（待ち行列）にアイテムを追加
            sub.cargoQueue.Enqueue(specificItemData);
            
            Debug.Log("アイテムを艦下部に一時回収！コンテナへ向かえ！");
            Destroy(gameObject);
        }
    }
}