using UnityEngine;
using System.Collections.Generic;
public class SubmarineStatus : MonoBehaviour
{
    public static SubmarineStatus Instance { get; private set; }
    
    [Header("Hull Status (耐久値)")]
    public float maxHP = 100f;
    public float currentHP = 100f;

    [Header("Movement (UI表示用)")]
    public float currentSpeed = 0f;      // 現在の速度
    public float currentTurnRate = 0f;   // 旋回速度

    [Header("Cargo & Repair")]
    // 拾ったアイテムのデータを入れておく「順番待ちの列」
    // 中身が null なら「ランダムアイテム」として扱います
    public Queue<ItemData> cargoQueue = new Queue<ItemData>();
    public RepairPoint[] repairPoints; // 艦内に配置した修復ポイントのリスト

    [Header("Upgrade Modifiers (強化補正)")]
    public float speedMultiplier = 1.0f; // 移動速度の倍率（1.0 = 100%）
    public float turnMultiplier = 1.0f;  // 旋回速度の倍率

    [Header("Unlocked Skills (解放済みスキル)")]
    public bool canUseTurbo = false;     // 例：ダッシュスキルが使えるか？
    public bool canUseDeepSonar = false; // 例：深海用ソナーが使えるか？
    

    private Vector3 lastPosition;
    private float lastYRotation;

    private Rigidbody rb; 

    void Awake()
    {
        // 💡 起動時に自分自身を登録
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 最初の角度を記憶
        lastYRotation = transform.eulerAngles.y;
        
        rb = GetComponentInParent<Rigidbody>(); 
    }

    void Update()
    {
        // =====================================
        // 1. スピードメーター（Rigidbodyから直接取得してブレを消す！）
        // =====================================
        if (rb != null)
        {
            // magnitude（ベクトルの長さ）を取得することで、物理エンジンが計算した
            // 「正確で全くブレない速度（絶対値）」をそのままUIに表示できます。
            currentSpeed = rb.linearVelocity.magnitude; 
        }

        // =====================================
        // 2. 旋回メーター（Updateで回転させているのでそのままでOK）
        // =====================================
        float currentY = transform.eulerAngles.y;
        float angleDelta = Mathf.DeltaAngle(lastYRotation, currentY);
        
        float targetTurnRate = angleDelta / Time.deltaTime;
        currentTurnRate = Mathf.Lerp(currentTurnRate, Mathf.Abs(targetTurnRate), Time.deltaTime * 5f);
        
        lastYRotation = currentY; 
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        BreakRandomRepairPoint();
    }

    public void RepairHull(float amount)
    {
        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;
    }

    private void BreakRandomRepairPoint()
    {
        if (repairPoints == null || repairPoints.Length == 0) return;

        // まだ壊れていない場所だけをリストアップする
        List<RepairPoint> intactPoints = new List<RepairPoint>();
        foreach (RepairPoint pt in repairPoints)
        {
            if (!pt.isBroken) intactPoints.Add(pt);
        }

        // 壊れる場所が残っていれば、ランダムに1つ選んで壊す
        if (intactPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, intactPoints.Count);
            intactPoints[randomIndex].SetBrokenState(true);
        }
    }

    [Header("パッシブ特殊効果フラグ")]
    public bool hasAutoRepair = false; // 自動修復パッシブを持っているか

    // 💡 汎用エフェクトパイプライン（一括処理用）
    // isApplying: trueなら効果を乗せる(装備/解放)、falseなら効果を引く(外す)
    public void ApplyEffects(List<SubmarineEffect> effects, bool isApplying)
    {
        float sign = isApplying ? 1f : -1f;

        foreach (var effect in effects)
        {
            float finalValue = effect.value * sign;

            switch (effect.effectType)
            {
                case SubmarineEffectType.MaxHP:
                    maxHP += finalValue;
                    currentHP = Mathf.Clamp(currentHP + finalValue, 0, maxHP);
                    Debug.Log($"パイプライン：最大HPが {finalValue} 変動しました。(現在:{maxHP})");
                    break;

                case SubmarineEffectType.SpeedMultiplier:
                    speedMultiplier += finalValue;
                    Debug.Log($"パイプライン：速度倍率が {finalValue} 変動しました。(現在:{speedMultiplier})");
                    break;

                case SubmarineEffectType.TurnMultiplier:
                    turnMultiplier += finalValue;
                    Debug.Log($"パイプライン：旋回倍率が {finalValue} 変動しました。(現在:{turnMultiplier})");
                    break;

                // ─── スキル・特殊効果系 ───
                case SubmarineEffectType.UnlockSkill_Turbo:
                    // スキルフラグは重複しないよう、適用時はtrue、外す時はfalseにする
                    canUseTurbo = isApplying;
                    Debug.Log($"パイプライン：ターボブーストフラグ = {canUseTurbo}");
                    break;

                case SubmarineEffectType.UnlockSkill_DeepSonar:
                    canUseDeepSonar = isApplying;
                    Debug.Log($"パイプライン：深海ソナーフラグ = {canUseDeepSonar}");
                    break;

                case SubmarineEffectType.Passive_AutoRepair:
                    hasAutoRepair = isApplying;
                    Debug.Log($"パイプライン：自動修復パッシブ = {hasAutoRepair}");
                    break;
            }
        }
    }
}