using UnityEngine;

public enum SubmarineTargetType
{
    Mine,
    HostileBio,
    NeutralBio,
    Item
}

public enum SonarTargetShape
{
    UI_Prefab,           // 機雷・アイテムなどの固定アイコン
    Procedural_Texture   // 生物などの滲む光の塊
}

public class SonarTarget : MonoBehaviour
{
    [Header("Target Settings")]
    public SubmarineTargetType targetType = SubmarineTargetType.Mine;
    public SonarTargetShape targetShape = SonarTargetShape.UI_Prefab;

    [Header("Damage Settings")]
    public float damageAmount = 25f;

    [Header("Item Settings")]
    public ItemData specificItemData;

    private void OnValidate()
    {
        // 敵生物なら自動でプロシージャル（滲む光）に設定
        if (targetType == SubmarineTargetType.HostileBio || targetType == SubmarineTargetType.NeutralBio)
        {
            targetShape = SonarTargetShape.Procedural_Texture;
        }
        else 
        {
            targetShape = SonarTargetShape.UI_Prefab;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        SubmarineStatus sub = other.GetComponentInParent<SubmarineStatus>();
        if (sub == null) return;

        if (targetType == SubmarineTargetType.Mine || targetType == SubmarineTargetType.HostileBio)
        {
            sub.TakeDamage(damageAmount);
            
            string attackName = targetType == SubmarineTargetType.Mine ? "機雷の爆発" : "巨大生物の激突";
            if (SubmarineHUD.Instance != null)
                SubmarineHUD.Instance.AddLog($"警告: {attackName} 船体に損傷あり", "#FF4444");
            
            Destroy(gameObject);
        }
        else if (targetType == SubmarineTargetType.Item)
        {
            sub.cargoQueue.Enqueue(specificItemData);
            
            if (SubmarineHUD.Instance != null)
                SubmarineHUD.Instance.AddLog("通知: コンテナ回収完了。", "#44FF44");
            
            Destroy(gameObject);
        }
    }
}