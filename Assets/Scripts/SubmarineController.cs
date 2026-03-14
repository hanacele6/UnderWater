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
        // ★ここを追加：マイナスの速度を持つ後進（リバース）ギア
        new EngineGear { gearName = "後進 (リバース)", maxSpeed = -2f, noiseMultiplier = 0.5f }, 

        // 以下は一つずつインデックスがずれます
        new EngineGear { gearName = "停止", maxSpeed = 0f, noiseMultiplier = 0.1f }, 
        new EngineGear { gearName = "1速 (静音)", maxSpeed = 2f, noiseMultiplier = 0.3f }, 
        new EngineGear { gearName = "2速 (巡航)", maxSpeed = 5f, noiseMultiplier = 1.0f }, 
        new EngineGear { gearName = "3速 (全速)", maxSpeed = 10f, noiseMultiplier = 2.0f } 
    };

    // ★変更：最初は「停止」にしておきたいので、初期インデックスを『1』にする
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
    }
    
    void Update()
    {
        if (!isPiloting) return;

        // ==========================================
        // 1. ギアの切り替え操作（Wでアップ、Sでダウン）
        // ==========================================
        // ※ここのロジックは一切変更しなくてOKです！
        // Sキーを押せばインデックスが減り、最終的に「0（後進）」に入ります。
        if (Input.GetKeyDown(KeyCode.W)) 
        {
            currentGearIndex = Mathf.Min(currentGearIndex + 1, gears.Length - 1);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            currentGearIndex = Mathf.Max(currentGearIndex - 1, 0);
        }

        // ==========================================
        // 2. 移動と旋回
        // ==========================================
        float targetSpeed = gears[currentGearIndex].maxSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);

        // ★マイナスの速度の時は、Vector3.forward にマイナスが掛けられるので自動的に後ろに進みます！
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // A/Dキーで旋回
        // もし「バック中はハンドルの向きを逆にしたい（車の挙動）」場合は、ここを少し改造します
        if (Input.GetKey(KeyCode.A)) transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.D)) transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
    }

    private void ChangeGear(int gearLevel)
    {
        if (gearLevel >= 0 && gearLevel < gears.Length)
        {
            currentGearIndex = gearLevel;
        }
    }
}