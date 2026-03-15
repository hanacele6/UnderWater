using UnityEngine;

public enum SubmarineTargetType
{
    Mine,
    HostileBio,
    NeutralBio,
    Item,
    Objective // これがミッション
}

public enum SonarTargetShape
{
    UI_Prefab,           // 従来のパルスで光るタイプ（機雷、アイテム用）
    Procedural_Texture,  // 滲む光の塊（生物用）
    AlwaysVisible_Marker // ★追加：ストラクチャーと同じく常に端に張り付くタイプ！
}

public class SonarTarget : MonoBehaviour
{
    [Header("Target Settings")]
    public SubmarineTargetType targetType = SubmarineTargetType.Mine;
    
    [Tooltip("ソナー上での見た目")]
    public SonarTargetShape targetShape = SonarTargetShape.UI_Prefab;

    [Header("Damage Settings")]
    public float damageAmount = 25f;

    [Header("Item Settings")]
    public ItemData specificItemData;

    [Header("目的地の表示設定（Objective用）")]
    public string targetLabel = ""; 
    public float areaRadius = 10f;

    private void OnValidate()
    {
        if (targetType == SubmarineTargetType.HostileBio || targetType == SubmarineTargetType.NeutralBio)
        {
            targetShape = SonarTargetShape.Procedural_Texture;
        }
        else if (targetType == SubmarineTargetType.Objective)
        {
            // ★変更：Objectiveの場合は、ストラクチャーと同じ常に表示されるマーカーにする
            targetShape = SonarTargetShape.AlwaysVisible_Marker;
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
            Debug.Log($"{attackName}！ ダメージ: {damageAmount} / 残りHP: {sub.currentHP}");
            
            if (SubmarineHUD.Instance != null)
                SubmarineHUD.Instance.AddLog($"警告: {attackName} 船体に損傷あり", "#FF4444");
            
            Destroy(gameObject);
        }
        else if (targetType == SubmarineTargetType.Item)
        {
            sub.cargoQueue.Enqueue(specificItemData);
            
            Debug.Log("アイテムを艦下部に一時回収！コンテナへ向かえ！");
            
            if (SubmarineHUD.Instance != null)
                SubmarineHUD.Instance.AddLog("通知: コンテナ回収完了。", "#44FF44");
            
            Destroy(gameObject);
        }
    }
}