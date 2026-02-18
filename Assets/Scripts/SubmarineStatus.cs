using UnityEngine;
using System.Collections.Generic;
public class SubmarineStatus : MonoBehaviour
{
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
    

    private Vector3 lastPosition;
    private float lastYRotation;

    void Start()
    {
        // 最初の位置と角度を記憶
        lastPosition = transform.position;
        lastYRotation = transform.eulerAngles.y;
    }

    void Update()
    {
        // =====================================
        // 1. スピードメーター（速度の計算）
        // =====================================
        // 1フレームでどれだけ移動したか（距離）を計算
        float distance = Vector3.Distance(transform.position, lastPosition);
        // 距離 ÷ 時間 で「秒速」を出す（ノット風にするなら数値を掛けて調整してください）
        float targetSpeed = distance / Time.deltaTime;
        
        // 数値がガクガク震えないように、Lerpで滑らかに変化させる
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);
        
        lastPosition = transform.position; // 記憶を更新

        // =====================================
        // 2. 旋回メーター（旋回速度の計算）
        // =====================================
        float currentY = transform.eulerAngles.y;
        // DeltaAngleを使うと、359度→0度を跨いだ時も正しく「1度動いた」と計算してくれます
        float angleDelta = Mathf.DeltaAngle(lastYRotation, currentY);
        
        float targetTurnRate = angleDelta / Time.deltaTime;
        currentTurnRate = Mathf.Lerp(currentTurnRate, Mathf.Abs(targetTurnRate), Time.deltaTime * 5f);
        
        lastYRotation = currentY; // 記憶を更新
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
}