using UnityEngine;

public class TestTube3D : MonoBehaviour
{
    [Header("連携設定")]
    [Tooltip("実験机を映している専用カメラ（DeskCamera）")]
    public Camera deskCamera;
    [Tooltip("フラスコの受け口（FlaskReceiver）")]
    public FlaskReceiver flaskReceiver;

    [Header("試験管の設定")]
    public float maxLiquid = 100f;
    public float currentLiquid;
    [Tooltip("1秒間に注ぐ量")]
    public float pourRatePerSecond = 30f;
    [Tooltip("フラスコの口にどれくらい近づけたら注ぐか")]
    public float pourDistance = 0.5f; // 3D空間の距離なので小さめ（0.5mなど）
    [Tooltip("注ぐ時の試験管の傾き（X, Y, Z）")]
    public Vector3 tiltRotation = new Vector3(0, 0, 45f); // 右に傾ける

    // ドラッグ用変数
    private Vector3 screenPoint;
    private Vector3 offset;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isDragging = false;

    void Start()
    {
        currentLiquid = maxLiquid;
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        // カメラがセットされていなければ、自動で探す
        if (deskCamera == null) deskCamera = GameObject.Find("DeskCamera").GetComponent<Camera>();
    }

    // ==========================================
    // 1. マウスで掴んだ瞬間
    // ==========================================
    void OnMouseDown()
    {
        // UI（会話やメニュー）の上なら掴まない
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        isDragging = true;
        
        // 3D空間の座標を、画面上の座標に変換してズレ（offset）を計算
        screenPoint = deskCamera.WorldToScreenPoint(gameObject.transform.position);
        offset = gameObject.transform.position - deskCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
    }

    // ==========================================
    // 2. マウスでドラッグしている間（毎フレーム）
    // ==========================================
    void OnMouseDrag()
    {
        if (!isDragging) return;

        // マウスの位置に合わせて試験管を空中で移動させる
        Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 cursorPosition = deskCamera.ScreenToWorldPoint(cursorPoint) + offset;
        transform.position = cursorPosition;

        // ---------- フラスコに注ぐ判定 ----------
        if (currentLiquid > 0 && flaskReceiver != null && !flaskReceiver.IsFull)
        {
            // 試験管とフラスコの口の距離を測る
            float distance = Vector3.Distance(transform.position, flaskReceiver.openingPoint.position);

            if (distance <= pourDistance)
            {
                // ① 傾ける
                transform.rotation = Quaternion.Euler(tiltRotation);

                // ② 液体を減らして、フラスコに送る
                float amountToPour = pourRatePerSecond * Time.deltaTime;
                if (currentLiquid < amountToPour) amountToPour = currentLiquid;
                
                currentLiquid -= amountToPour;
                flaskReceiver.ReceiveLiquid(amountToPour);

                // ここで音や、液体のパーティクルを出す
            }
            else
            {
                // 離れたら傾きを元に戻す
                transform.rotation = startRotation;
            }
        }
    }

    // ==========================================
    // 3. マウスを離した瞬間
    // ==========================================
    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        // 試験管立ての位置と傾きにカチャッと戻す
        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}