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
    public float pourDistance = 0.5f; 
    
    [Tooltip("注ぐ時の「最大」傾き（X, Y, Z）")]
    public Vector3 maxTiltRotation = new Vector3(0, 0, 75f); // 少し深めに設定
    [Tooltip("傾くスピード")]
    public float tiltSpeed = 5f;

    [Header("3Dシェーダー・演出設定")]
    [Tooltip("試験管の中の液体メッシュ（Renderer）をアサイン")]
    public Renderer liquidRenderer;
    [Tooltip("口に付けた注ぐ水流（Particle System）をアサイン")]
    public ParticleSystem pourParticles;

    // ドラッグ用変数
    private Vector3 screenPoint;
    private Vector3 offset;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isDragging = false;

    // シェーダー制御用変数
    private const string FillLevelProp = "_FillLevel";
    private Material liquidMat;
    private float initialMeshHeight;

    private float localBottomY;
    private float localTopY;

    void Start()
    {
        //currentLiquid = maxLiquid;
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        if (deskCamera == null) deskCamera = GameObject.Find("DeskCamera").GetComponent<Camera>();
        if (liquidRenderer != null) liquidMat = liquidRenderer.material;
    }

    void Update()
    {
        if (liquidMat != null && liquidRenderer != null)
        {
            float ratio = currentLiquid / maxLiquid;
            
            // 試験管が傾いて bounds（枠）が縮んでも、常にその時の下と上を使ってLerpする！
            float currentWorldY = Mathf.Lerp(liquidRenderer.bounds.min.y, liquidRenderer.bounds.max.y, ratio);
            
            liquidMat.SetFloat(FillLevelProp, currentWorldY);
        }
    }


    // ==========================================
    // 1. マウスで掴んだ瞬間
    // ==========================================
    void OnMouseDown()
    {
        if (currentLiquid <= 0f) return;
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        isDragging = true;
        
        screenPoint = deskCamera.WorldToScreenPoint(gameObject.transform.position);
        offset = gameObject.transform.position - deskCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
    }

    // ==========================================
    // 2. マウスでドラッグしている間（毎フレーム）
    // ==========================================
    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 cursorPosition = deskCamera.ScreenToWorldPoint(cursorPoint) + offset;
        transform.position = cursorPosition;

        // ---------- フラスコに注ぐ判定 ----------
        if (flaskReceiver != null)
        {
            float distance = Vector3.Distance(transform.position, flaskReceiver.openingPoint.position);

            if (distance <= pourDistance)
            {
                float tiltRatio = 1.0f - (distance / pourDistance); 
                Quaternion targetRot = Quaternion.Euler(maxTiltRotation);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * tiltSpeed);


                if (currentLiquid > 0f && !flaskReceiver.IsFull)
                {
                    if (pourParticles != null && !pourParticles.isPlaying) pourParticles.Play();

                    float amountToPour = pourRatePerSecond * Time.deltaTime;
                    if (currentLiquid < amountToPour) amountToPour = currentLiquid;
                    
                    currentLiquid -= amountToPour;
                    flaskReceiver.ReceiveLiquid(amountToPour);
                    UpdateShaderFillLevel();

                    if (currentLiquid <= 0f)
                    {
                        currentLiquid = 0f; // 絶対にマイナスにさせない
                        if (pourParticles != null) pourParticles.Stop();
                        Debug.Log("試験管が完全に空になりました！");
                    }
                }
                else
                {
                    // 空っぽ、またはフラスコが満タンの場合は絶対にパーティクルを出さない
                    if (pourParticles != null && pourParticles.isPlaying) pourParticles.Stop();
                }
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, startRotation, Time.deltaTime * tiltSpeed);
                if (pourParticles != null && pourParticles.isPlaying) pourParticles.Stop();
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

        // 手を離したら瞬時にカチャッと元の位置・角度に戻る
        transform.position = startPosition;
        transform.rotation = startRotation;

        // パーティクルを確実に止める
        if (pourParticles != null && pourParticles.isPlaying) pourParticles.Stop();
    }


    private void UpdateShaderFillLevel()
    {
        if (liquidMat != null)
        {
            float ratio = currentLiquid / maxLiquid;
            
            float localFillY = Mathf.Lerp(localBottomY, localTopY, ratio);
            
            liquidMat.SetFloat(FillLevelProp, localFillY);
        }
    }
}