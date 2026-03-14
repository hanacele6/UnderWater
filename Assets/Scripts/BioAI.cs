using UnityEngine;

[RequireComponent(typeof(SonarTarget))]
public class BioAI : MonoBehaviour
{
    [Header("AI Settings")]
    public Transform player;

    [Header("AI State")]
    public bool isAIActive = true;
    public float wanderSpeed = 2f;
    public float sprintSpeed = 5f;
    public float turnSpeed = 2f;
    
    [Header("Obstacle Avoidance")]
    public LayerMask wallLayer;

    private SonarTarget sonarTarget;
    private Vector3 wanderDestination;
    private float wanderTimer;
    private SubmarineController playerSubmarine; 

    void Start()
    {
        sonarTarget = GetComponent<SonarTarget>();
        
        if (player == null)
        {
            SubmarineStatus sub = FindObjectOfType<SubmarineStatus>();
            if (sub != null) player = sub.transform;
        }

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

        float currentNoiseRadius = 0f;
        if (playerSubmarine != null)
        {
            currentNoiseRadius = playerSubmarine.GetCurrentNoiseRadius();
        }

        // 自分がその「騒音の円」の中に入ってしまったらバレる！
        if (distanceToPlayer <= currentNoiseRadius)
        {
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
            Wander();
        }
    }

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

            // 壁にぶつかった時、プレイヤーの範囲外なら新しい場所へ
            if (playerSubmarine != null && Vector3.Distance(transform.position, player.position) > playerSubmarine.GetCurrentNoiseRadius())
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