using UnityEngine;

public class TestTube3D : MonoBehaviour
{
    [Header("連携設定")]
    public Camera deskCamera;
    public FlaskReceiver flaskReceiver;

    [Header("試験管の設定")]
    public float maxLiquid = 100f;
    public float currentLiquid;
    public float pourRatePerSecond = 30f;
    public float pourDistance = 0.5f; 
    
    [Header("注ぎ口の設定")]
    public Transform spoutPoint;
    public Vector3 pourOffset = new Vector3(0, 0.5f, 0);

    public Vector3 maxTiltRotation = new Vector3(0, 0, 75f);
    public float tiltSpeed = 5f;

    [Header("3Dシェーダー・演出設定")]
    public Renderer liquidRenderer;
    public ParticleSystem pourParticles;

    // ドラッグ用変数
    private Vector3 screenPoint;
    private Vector3 offset;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isDragging = false;
    
    // 💡追加：吸い付き状態を管理するフラグ
    private bool isSnapped = false;

    // シェーダー制御用変数
    private const string FillLevelProp = "_FillLevel";
    private Material liquidMat;

    // 💡追加：ハイライト制御用
    private InteractableHighlight highlight;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        if (deskCamera == null) deskCamera = GameObject.Find("DeskCamera").GetComponent<Camera>();
        if (liquidRenderer != null) liquidMat = liquidRenderer.material;
        
        // 💡 ハイライトスクリプトを取得
        highlight = GetComponentInChildren<InteractableHighlight>();
    }

    void Update()
    {
        if (liquidMat != null && liquidRenderer != null)
        {
            float ratio = currentLiquid / maxLiquid;
            float currentWorldY = Mathf.Lerp(liquidRenderer.bounds.min.y, liquidRenderer.bounds.max.y, ratio);
            liquidMat.SetFloat(FillLevelProp, currentWorldY);
        }
    }

    void OnMouseDown()
    {
        if (currentLiquid <= 0f) return;
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        isDragging = true;
        isSnapped = false; // 掴んだ時はリセット
        
        screenPoint = deskCamera.WorldToScreenPoint(gameObject.transform.position);
        offset = gameObject.transform.position - deskCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));

        // 💡 ハイライトを強制オフ（抑制）
        if (highlight != null) 
        {
            highlight.isSuppressed = true;
            highlight.ChangeHighlightState(InteractableHighlight.HighlightState.None);
        }
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 cursorPosition = deskCamera.ScreenToWorldPoint(cursorPoint) + offset;

        if (flaskReceiver != null)
        {
            Transform referencePoint = spoutPoint != null ? spoutPoint : transform;
            Vector3 spoutLocalOffset = referencePoint.position - transform.position;
            
            // 仮想の注ぎ口の位置
            Vector3 virtualSpoutPos = cursorPosition + spoutLocalOffset;
            float distance = Vector3.Distance(virtualSpoutPos, flaskReceiver.openingPoint.position);

            // 💡 修正のコア：「遊び」を持たせたスナップ判定（速度チェックは削除！）
            if (isSnapped)
            {
                // 既に吸い付いている場合は、設定距離の「1.5倍」離さないと引き剥がせない（これが磁石感を生む）
                if (distance > pourDistance * 1.5f) isSnapped = false;
            }
            else
            {
                // まだ吸い付いていない場合は、設定距離に入ったら吸い付く
                if (distance <= pourDistance) isSnapped = true;
            }

            // --- 判定結果に基づく処理 ---
            if (isSnapped)
            {
                // 吸い付き＆注ぐ処理
                Vector3 targetPos = flaskReceiver.openingPoint.position + pourOffset - spoutLocalOffset;
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);

                Quaternion targetRot = Quaternion.Euler(maxTiltRotation);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * tiltSpeed);

                if (currentLiquid > 0f && !flaskReceiver.IsFull && !flaskReceiver.isMixingComplete)
                {
                    if (pourParticles != null && !pourParticles.isPlaying) pourParticles.Play();

                    float amountToPour = pourRatePerSecond * Time.deltaTime;
                    if (currentLiquid < amountToPour) amountToPour = currentLiquid;
                    
                    currentLiquid -= amountToPour;
                    flaskReceiver.ReceiveLiquid(amountToPour);
                    
                    if (currentLiquid <= 0f)
                    {
                        currentLiquid = 0f;
                        if (pourParticles != null) pourParticles.Stop();
                    }
                }
                else
                {
                    if (pourParticles != null && pourParticles.isPlaying) pourParticles.Stop();
                }
            }
            else
            {
                // 追従処理（スナップ解除時）
                transform.position = Vector3.Lerp(transform.position, cursorPosition, Time.deltaTime * 15f);
                transform.rotation = Quaternion.Slerp(transform.rotation, startRotation, Time.deltaTime * tiltSpeed);
                if (pourParticles != null && pourParticles.isPlaying) pourParticles.Stop();
            }
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, cursorPosition, Time.deltaTime * 15f);
        }
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;
        isSnapped = false; // 離した時もリセット

        transform.position = startPosition;
        transform.rotation = startRotation;

        if (pourParticles != null && pourParticles.isPlaying) pourParticles.Stop();

        // 💡 ハイライトの抑制を解除
        if (highlight != null) highlight.isSuppressed = false;
    }
}