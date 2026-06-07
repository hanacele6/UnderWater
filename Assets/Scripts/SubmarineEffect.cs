using UnityEngine;

// 💡 潜水艦に与えられるあらゆる効果のカタログ
public enum SubmarineEffectType
{
    // ─── 通常のステータス強化 ───
    MaxHP,
    SpeedMultiplier,
    TurnMultiplier,

    // ─── 特殊効果・パッシブ・スキル解放（ここへ無限に追加可能） ───
    UnlockSkill_Turbo,         // ターボブースト解放
    UnlockSkill_DeepSonar,     // 深海ソナー解放
    Passive_AutoRepair,        // 時間経過での自動装甲修復
    Passive_SonarRangeBoost    // ソナー範囲の超拡大
}

[System.Serializable]
public struct SubmarineEffect
{
    [Tooltip("効果の種類")]
    public SubmarineEffectType effectType;
    
    [Tooltip("効果の大きさ（ステータスなら加算値、スキル解放なら1でON/0でOFFなど）")]
    public float value;
}