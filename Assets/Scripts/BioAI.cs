using UnityEngine;

[RequireComponent(typeof(SonarTarget))] // SonarTargetスクリプトとセットで動くようにする
public class BioAI : MonoBehaviour
{
    [Header("AI Settings")]
    [Tooltip("プレイヤーの潜水艦（自動で取得しますが、手動設定も可）")]
    public Transform player;

    [Header("AI State")]
    [Tooltip("チェックが外れると、思考も移動も完全に停止します")]
    public bool isAIActive = true;
    
    [Tooltip("通常の泳ぐ速さ")]
    public float wanderSpeed = 2f;
    [Tooltip("追跡・逃走時の本気の速さ")]
    public float sprintSpeed = 5f;
    [Tooltip("旋回スピード（滑らかさ）")]
    public float turnSpeed = 2f;
    
    [Tooltip("プレイヤーに気づく距離（ソナーの範囲より少し狭いとホラー感が出ます）")]
    public float detectionRadius = 30f;

    [Header("Obstacle Avoidance (障害物回避)")]
    [Tooltip("壁として判定するレイヤー（SonarManagerのWallLayerと同じものを指定）")]
    public LayerMask wallLayer;

    private SonarTarget sonarTarget;
    private Vector3 wanderDestination;
    private float wanderTimer;

    void Start()
    {
        sonarTarget = GetComponent<SonarTarget>();
        
        // プレイヤーが未設定の場合、SubmarineStatusを探して自動取得する
        if (player == null)
        {
            SubmarineStatus sub = FindObjectOfType<SubmarineStatus>();
            if (sub != null) player = sub.transform;
        }

        SetNewWanderDestination();
    }

    void Update()
    {

        if (!isAIActive) return;

        // 機雷（Mine）ならAIは何もしない
        if (sonarTarget.targetType == SubmarineTargetType.Mine || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // --- 状態の判定 ---
        if (distanceToPlayer <= detectionRadius)
        {
            // プレイヤーを感知した！
            if (sonarTarget.targetType == SubmarineTargetType.HostileBio)
            {
                // 敵性：プレイヤーに向かって突撃（Chase）
                MoveTowards(player.position, sprintSpeed);
            }
            else if (sonarTarget.targetType == SubmarineTargetType.NeutralBio)
            {
                // 中立：プレイヤーから逃げる（Flee）
                Vector3 fleeDirection = (transform.position - player.position).normalized;
                MoveTowards(transform.position + fleeDirection * 10f, sprintSpeed);
            }
        }
        else
        {
            // 感知範囲外：のんびり徘徊する（Wander）
            Wander();
        }
    }

    // ==========================================
    // 行動ロジック
    // ==========================================
    private void Wander()
    {
        wanderTimer -= Time.deltaTime;
        // 目的地に着くか、一定時間経ったら新しい目的地を決める
        if (wanderTimer <= 0f || Vector3.Distance(transform.position, wanderDestination) < 2f)
        {
            SetNewWanderDestination();
        }

        MoveTowards(wanderDestination, wanderSpeed);
    }

    private void SetNewWanderDestination()
    {
        // 今いる場所から半径20m以内のランダムな場所を次の目的地にする
        Vector2 randomCircle = Random.insideUnitCircle * 20f;
        wanderDestination = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        wanderTimer = Random.Range(5f, 10f); // 5〜10秒ごとに気まぐれで方向転換
    }

    private void MoveTowards(Vector3 targetPos, float speed)
    {
        targetPos.y = transform.position.y;
        Vector3 direction = (targetPos - transform.position).normalized;

        // ==========================================
        // 障害物センサー（Raycast）
        // ==========================================
        // 自分の前方にヒゲ（Ray）を伸ばし、10m以内に壁（wallLayer）があるかチェック！
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 10f, wallLayer))
        {
            // 壁があったら、壁の表面に対して「垂直な方向（法線）」を足して、壁から離れるように進路を曲げる
            direction += hit.normal * 2f;
            direction.y = 0; // 上下には行かないよう固定
            direction.Normalize();

            // もし「徘徊中」に壁にぶつかりそうになったら、そもそも目的地を別の場所に変えちゃう
            if (Vector3.Distance(transform.position, player.position) > detectionRadius)
            {
                SetNewWanderDestination();
            }
        }

        // ==========================================
        // 旋回と移動
        // ==========================================
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }

        // ★追加：壁への「めり込み防止」の最終確認
        // 自分が今から進む歩幅（moveStep）のすぐ先に壁がないかチェック。壁がなければ進む。
        float moveStep = speed * Time.deltaTime;
        if (!Physics.Raycast(transform.position, transform.forward, moveStep + 1.0f, wallLayer))
        {
            transform.position += transform.forward * moveStep;
        }
    }
}