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
        public string gearName;       // ギアの名前（例: "1速(微速)"）
        public float maxSpeed;        // このギアでの最高速度
        public float noiseMultiplier; // 音の大きさ（AIの感知半径にかかる倍率。0.3なら感知範囲が30%になる）
    }

    [Header("ギア一覧（0=停止, 1=1速, 2=2速...）")]
    public EngineGear[] gears = new EngineGear[]
    {
        new EngineGear { gearName = "停止", maxSpeed = 0f, noiseMultiplier = 0.1f },  // ほぼ無音
        new EngineGear { gearName = "1速 (静音)", maxSpeed = 2f, noiseMultiplier = 0.3f },  // ステルス（30%の距離までバレない）
        new EngineGear { gearName = "2速 (巡航)", maxSpeed = 5f, noiseMultiplier = 1.0f },  // 通常
        new EngineGear { gearName = "3速 (全速)", maxSpeed = 10f, noiseMultiplier = 2.0f }  // 爆音（通常の2倍遠くからバレる）
    };

    public int currentGearIndex = 0;

    // AIが読み取るための「現在の騒音レベル」
    public float GetCurrentNoiseMultiplier()
    {
        return gears[currentGearIndex].noiseMultiplier;
    }

    public static SubmarineController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    

    void Update()
    {
        // ★ここが超重要：操縦中（ソナーを開いている時）でなければ、操作を一切受け付けない
        if (!isPiloting) return;

        // ==========================================
        // 1. ギアの切り替え操作（Wでアップ、Sでダウン）
        // ==========================================
        if (Input.GetKeyDown(KeyCode.W)) 
        {
            // シフトアップ：1足すが、最大ギア（gearsの数 - 1）以上にはならないようにする
            currentGearIndex = Mathf.Min(currentGearIndex + 1, gears.Length - 1);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            // シフトダウン：1引くが、0（停止）未満にはならないようにする
            currentGearIndex = Mathf.Max(currentGearIndex - 1, 0);
        }

        // ==========================================
        // 2. 移動と旋回
        // ==========================================
        // 現在のギアの最高速度に向かって、滑らかに加速・減速する
        float targetSpeed = gears[currentGearIndex].maxSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);

        // 常に前進し続ける（0速ならtargetSpeedが0なので自然と止まる）
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // A/Dキーで旋回
        if (Input.GetKey(KeyCode.A)) transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.D)) transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
    }

    private void ChangeGear(int gearLevel)
    {
        // 設定されているギアの数を超えないようにする
        if (gearLevel >= 0 && gearLevel < gears.Length)
        {
            currentGearIndex = gearLevel;
            // 必要であれば、ここに「UIのテキストを更新する」処理を入れると分かりやすいです
            // 例: UIManager.Instance.ShowMessage($"エンジン: {gears[currentGearIndex].gearName}");
        }
    }
}