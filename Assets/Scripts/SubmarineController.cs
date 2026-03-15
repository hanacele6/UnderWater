using UnityEngine;

public class SubmarineController : MonoBehaviour
{
    [Header("移動・ギア設定")]
    public float currentSpeed = 0f; // 現在の実際の速度
    public float turnSpeed = 30f;   // 旋回スピード
    public float acceleration = 2f; // 加速・減速の滑らかさ

    public bool isPiloting = false; 

    // ==========================================
    // ギアシステムの定義
    // ==========================================
    [System.Serializable]
    public struct EngineGear
    {
        public string gearName;       // ギアの名前
        public float maxSpeed;        // このギアでの最高速度（マイナスならバック）
        public float noiseMultiplier; // 音の大きさ
    }

    [Header("ギア一覧（0=後進, 1=停止, 2=1速...）")]
    public EngineGear[] gears = new EngineGear[]
    {
        new EngineGear { gearName = "後進 (リバース)", maxSpeed = -2f, noiseMultiplier = 0.3f }, 

        new EngineGear { gearName = "停止", maxSpeed = 0f, noiseMultiplier = 0.1f }, 
        new EngineGear { gearName = "1速 (静音)", maxSpeed = 2f, noiseMultiplier = 0.3f }, 
        new EngineGear { gearName = "2速 (巡航)", maxSpeed = 5f, noiseMultiplier = 1.0f }, 
        new EngineGear { gearName = "3速 (全速)", maxSpeed = 10f, noiseMultiplier = 2.0f } 
    };
    private Rigidbody rb;
    public int currentGearIndex = 1;

    [Header("Stealth & Noise")]
    [Tooltip("通常時（倍率1.0）の時に、敵にバレる基本の距離")]
    public float baseNoiseRadius = 20f;

    // AIが読み取るための「現在の騒音レベル」
    public float GetCurrentNoiseMultiplier()
    {
        return gears[currentGearIndex].noiseMultiplier;
    }

    public float GetCurrentNoiseRadius()
    {
        return baseNoiseRadius * GetCurrentNoiseMultiplier();
    }

    public static SubmarineController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        rb = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        if (!isPiloting) return;

        if (Input.GetKeyDown(KeyCode.W)) 
        {
            currentGearIndex = Mathf.Min(currentGearIndex + 1, gears.Length - 1);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            currentGearIndex = Mathf.Max(currentGearIndex - 1, 0);
        }

        float targetSpeed = gears[currentGearIndex].maxSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);

        // 旋回入力の判定
        float turnInput = 0f;
        if (Input.GetKey(KeyCode.A)) turnInput = -1f;
        if (Input.GetKey(KeyCode.D)) turnInput = 1f;

        if (currentSpeed < -0.1f) turnInput *= -1f;

        if (turnInput != 0f)
        {
            transform.Rotate(Vector3.up, turnInput * turnSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // 操縦していない時は、速度をゼロにして惰性を消す
        if (!isPiloting)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * acceleration);
            return;
        }

        rb.linearVelocity = transform.forward * currentSpeed;
    }

    private void ChangeGear(int gearLevel)
    {
        if (gearLevel >= 0 && gearLevel < gears.Length)
        {
            currentGearIndex = gearLevel;
        }
    }
}