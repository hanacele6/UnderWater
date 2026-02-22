using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MissionAreaTrigger : MonoBehaviour
{
    public enum AreaType
    {
        ReachArea,      // ① ただ到達するだけ
        StayAndScan,    // ② 指定エリア内に数秒間留まる（環境スキャン）
        StealthPass     // ③ 低速で静かに進入する（ステルス）
    }

    [Header("目的ギミックの設定")]
    public AreaType areaType = AreaType.ReachArea;
    public string flagToSet; // 達成時にGameManagerに送るフラグ名
    public string reachMessage = "目標を達成した";

    [Header("ギミック詳細（②・③の場合のみ使用）")]
    public float requiredStayTime = 3f; // ②スキャン完了に必要な秒数
    public float maxStealthSpeed = 2f;  // ③ステルスとみなされる最大速度

    [Header("出現条件（ソナーへの表示タイミング）")]
    [Tooltip("このフェーズ以降になったらソナーに映る（例：Operation）")]
    public GamePhase activePhase = GamePhase.Operation;
    
    [Tooltip("このフラグが立っている時だけ出現する（空欄ならフェーズ条件のみ）")]
    public string requiredFlagToAppear;

    private float currentStayTime = 0f;
    private bool isCleared = false;
    
    // コンポーネントの参照
    private Collider myCollider;
    private SonarTarget mySonarTarget; // ソナーに映すためのコンポーネント

    void Start()
    {
        myCollider = GetComponent<Collider>();
        myCollider.isTrigger = true; // 物理的にぶつからないようにする
        
        // 同じオブジェクトについているSonarTargetを取得しておく
        mySonarTarget = GetComponent<SonarTarget>();
        
        if (GetComponent<MeshRenderer>() != null) GetComponent<MeshRenderer>().enabled = false;
    }

    void Update()
    {
        if (isCleared || GameManager.Instance == null) return;

        // ==========================================
        // 表示・判定タイミングの自動制御
        // ==========================================
        bool isPhaseOK = GameManager.Instance.currentPhase >= activePhase;
        bool isFlagOK = string.IsNullOrEmpty(requiredFlagToAppear) || GameManager.Instance.GetFlag(requiredFlagToAppear);
        
        bool shouldBeActive = isPhaseOK && isFlagOK;

        // 条件を満たしていない時は、当たり判定もソナー表示も「オフ」にする！
        if (myCollider.enabled != shouldBeActive)
        {
            myCollider.enabled = shouldBeActive;
            if (mySonarTarget != null) mySonarTarget.enabled = shouldBeActive;
        }
    }

    // ==========================================
    // 潜水艦がエリアに入っている間の処理
    // ==========================================
    void OnTriggerStay(Collider other)
    {
        if (isCleared || !myCollider.enabled) return;

        if (other.CompareTag("Player") || other.GetComponent<SubmarineController>() != null)
        {
            switch (areaType)
            {
                case AreaType.ReachArea:
                    // ① 入った瞬間にクリア
                    CompleteMission();
                    break;

                case AreaType.StayAndScan:
                    // ② エリア内にいる間、タイマーを進める
                    currentStayTime += Time.deltaTime;
                    if (currentStayTime >= requiredStayTime) CompleteMission();
                    break;

                case AreaType.StealthPass:
                    // ③ 進入時の速度が設定以下ならクリア（※SubmarineControllerから速度を取得する想定）
                    // SubmarineController sub = other.GetComponent<SubmarineController>();
                    // if (sub != null && sub.currentSpeed <= maxStealthSpeed) { CompleteMission(); }
                    
                    // ※仮実装：とりあえず入ったらクリア扱いにしています。
                    // 速度制御が完成したら上のコメントアウトを外してください。
                    CompleteMission();
                    break;
            }
        }
    }

    // エリアから出てしまったら、スキャン時間をリセットする
    void OnTriggerExit(Collider other)
    {
        if (areaType == AreaType.StayAndScan && (other.CompareTag("Player") || other.GetComponent<SubmarineController>() != null))
        {
            currentStayTime = 0f;
        }
    }

    // ==========================================
    // 目的達成処理
    // ==========================================
    private void CompleteMission()
    {
        isCleared = true;
        GameManager.Instance.SetFlag(flagToSet, true);
        
        if (!string.IsNullOrEmpty(reachMessage) && UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage(reachMessage);
        }

        gameObject.SetActive(false); // 完全に消去する
    }
}