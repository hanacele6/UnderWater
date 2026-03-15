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

    private Rigidbody rb; 

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
}