using UnityEngine;

[RequireComponent(typeof(SonarTarget))]
public class BioAI : MonoBehaviour
{
    [Header("AI Settings")]
    [Tooltip("プレイヤーの潜水艦（自動で取得しますが、手動設定も可）")]
    public Transform player;

    [Header("AI State")]
    public bool isAIActive = true;
    public float wanderSpeed = 2f;
    public float sprintSpeed = 5f;
    public float turnSpeed = 2f;
    
    [Tooltip("本来のプレイヤーに気づく距離（この数値に、潜水艦の騒音倍率が掛けられます）")]
    public float baseDetectionRadius = 30f; // ★名前を少し分かりやすく変更

    [Header("Obstacle Avoidance (障害物回避)")]
    public LayerMask wallLayer;

    private SonarTarget sonarTarget;
    private Vector3 wanderDestination;
    private float wanderTimer;

    // ★追加：プレイヤーの騒音を取得するための参照
    private SubmarineController playerSubmarine; 

    void Start()
    {
        sonarTarget = GetComponent<SonarTarget>();
        
        // プレイヤーが未設定の場合、自動取得する
        if (player == null)
        {
            SubmarineStatus sub = FindObjectOfType<SubmarineStatus>();
            if (sub != null) player = sub.transform;
        }

        // ★追加：SubmarineControllerを取得しておく
        if (player != null)
        {
            playerSubmarine = player.GetComponent<SubmarineController>();
        }

        SetNewWanderDestination();
    }

    void Update()
    {
        if (!isAIActive) return;
        if (sonarTarget.targetType == SubmarineTargetType.Mine || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // ==========================================
        // ★修正：プレイヤーの騒音レベルを考慮した感知距離の計算
        // ==========================================
        float currentDetectionRadius = baseDetectionRadius;

        if (playerSubmarine != null)
        {
            // 潜水艦の現在のギアによる「騒音倍率（0.3など）」を掛ける！
            currentDetectionRadius *= playerSubmarine.GetCurrentNoiseMultiplier();
        }

        // --- 状態の判定 ---
        // 計算した結果の「現在の感知距離」より近づいてしまったらバレる！
        if (distanceToPlayer <= currentDetectionRadius)
        {
            // プレイヤーを感知した！
            if (sonarTarget.targetType == SubmarineTargetType.HostileBio)
            {
                MoveTowards(player.position, sprintSpeed);
            }
            else if (sonarTarget.targetType == SubmarineTargetType.NeutralBio)
            {
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
    // 行動ロジック（※以下は元のまま変更なし！）
    // ==========================================
    private void Wander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || Vector3.Distance(transform.position, wanderDestination) < 2f)
        {
            SetNewWanderDestination();
        }
        MoveTowards(wanderDestination, wanderSpeed);
    }

    private void SetNewWanderDestination()
    {
        Vector2 randomCircle = Random.insideUnitCircle * 20f;
        wanderDestination = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        wanderTimer = Random.Range(5f, 10f);
    }

    private void MoveTowards(Vector3 targetPos, float speed)
    {
        targetPos.y = transform.position.y;
        Vector3 direction = (targetPos - transform.position).normalized;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 10f, wallLayer))
        {
            direction += hit.normal * 2f;
            direction.y = 0; 
            direction.Normalize();

            if (Vector3.Distance(transform.position, player.position) > baseDetectionRadius)
            {
                SetNewWanderDestination();
            }
        }

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }

        float moveStep = speed * Time.deltaTime;
        if (!Physics.Raycast(transform.position, transform.forward, moveStep + 1.0f, wallLayer))
        {
            transform.position += transform.forward * moveStep;
        }
    }
}