using UnityEngine;

public class FlaskShaker : MonoBehaviour
{
    [Header("階層の参照")]
    public Transform flaskRoot;  
    public Transform flaskPivot; 
    public Camera deskCamera;

    [Header("注ぎ口の設定")]
    public Transform spoutPoint; 
    public Vector3 pourOffset = new Vector3(0, 0.4f, 0); 

    [Header("ふりこの物理設定")]
    public float swingSensitivity = 20f; 
    public float gravity = 150f;
    public float damping = 5f; 
    public float maxTiltAngle = 60f;

    [Header("注ぎ動作設定（ピペット用）")]
    public float pourRatePerSecond = 20f;
    public float pourDistance = 1.0f; // この距離に入ったら吸い付く
    public float searchRadius = 2.0f; // ピペットを探すレーダーの半径
    public float pourTiltAngle = 100f; // 注ぐ時の傾き
    public float tiltSpeed = 10f;
    public ParticleSystem pourParticles;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isDragging = false;
    private float zDistance;
    private Vector3 dragOffset;

    private float angleX, angleZ, velX, velZ;
    private MonoBehaviour highlight; // 型は実際のハイライトスクリプトに合わせてください

    void Start()
    {
        initialPosition = flaskRoot.position;
        initialRotation = flaskPivot.localRotation;
        if (deskCamera == null) deskCamera = GameObject.Find("DeskCamera")?.GetComponent<Camera>();
        highlight = flaskRoot.GetComponentInChildren<InteractableHighlight>();
    }

    void Update()
    {
        HandleInput();
        if (isDragging) PerformAction();
        else ReturnToDefault();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject() == true) return;
            Ray ray = deskCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(flaskRoot))
                {
                    isDragging = true;
                    zDistance = deskCamera.WorldToScreenPoint(flaskRoot.position).z;
                    Vector3 mouseWorld = deskCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
                    dragOffset = flaskRoot.position - mouseWorld;

                    if (highlight != null) ((InteractableHighlight)highlight).isSuppressed = true;
                    
                    if (FlaskReceiver.Instance != null && !FlaskReceiver.Instance.isMixingComplete)
                    {
                        FlaskReceiver.Instance.SetProgressBarVisible(true); 
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            if (highlight != null) ((InteractableHighlight)highlight).isSuppressed = false;
            if (pourParticles != null && pourParticles.isPlaying) pourParticles.Stop();
            if (FlaskReceiver.Instance != null) FlaskReceiver.Instance.SetProgressBarVisible(false);
        }
    }

    private void PerformAction()
    {
        Vector3 mouseWorld = deskCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));
        Vector3 targetPos = mouseWorld + dragOffset;

        if (FlaskReceiver.Instance != null && FlaskReceiver.Instance.isMixingComplete)
        {
            PerformPour(targetPos); // 完成したら注ぎモード
        }
        else
        {
            PerformShake(targetPos); // それ以外は振り子モード
        }
    }

    private void PerformShake(Vector3 targetPos)
    {
        flaskRoot.position = targetPos; // 常にマウスに追従

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if ((Mathf.Abs(mouseX) > 0.1f || Mathf.Abs(mouseY) > 0.1f) && FlaskReceiver.Instance != null)
            FlaskReceiver.Instance.AddMixProgress((Mathf.Abs(mouseX) + Mathf.Abs(mouseY)) * 2f);

        velX += mouseY * swingSensitivity;
        velZ -= mouseX * swingSensitivity;
        velX -= angleX * gravity * Time.deltaTime;
        velZ -= angleZ * gravity * Time.deltaTime;
        float drag = Mathf.Clamp01(1f - damping * Time.deltaTime);
        velX *= drag; velZ *= drag;
        angleX += velX * Time.deltaTime;
        angleZ += velZ * Time.deltaTime;
        angleX = Mathf.Clamp(angleX, -maxTiltAngle, maxTiltAngle);
        angleZ = Mathf.Clamp(angleZ, -maxTiltAngle, maxTiltAngle);

        flaskPivot.localRotation = initialRotation * Quaternion.Euler(angleX, 0, angleZ);
    }

    private void PerformPour(Vector3 targetPos)
    {
        Transform referencePoint = spoutPoint != null ? spoutPoint : flaskRoot;
        
        Collider[] hitColliders = Physics.OverlapSphere(referencePoint.position, searchRadius);
        PipetteReceiver nearestPipette = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hitColliders)
        {
            PipetteReceiver pipette = hit.GetComponentInParent<PipetteReceiver>();
            if (pipette != null && !pipette.IsFull)
            {
                float dist = Vector3.Distance(referencePoint.position, pipette.openingPoint.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestPipette = pipette;
                }
            }
        }

        // 💡 近づいた＆ピペットがまだ満タンじゃない時だけ吸い付いて注ぐ！
        if (nearestPipette != null && minDistance <= pourDistance)
        {
            Vector3 snapPos = nearestPipette.openingPoint.position + pourOffset - (referencePoint.position - flaskRoot.position);
            flaskRoot.position = Vector3.Lerp(flaskRoot.position, snapPos, Time.deltaTime * 5f);

            Quaternion targetRot = Quaternion.Euler(pourTiltAngle, 0, 0); 
            flaskPivot.localRotation = Quaternion.Slerp(flaskPivot.localRotation, targetRot, Time.deltaTime * tiltSpeed);

            // --- 💡 ここから：連続で注ぐ処理 ---
            if (pourParticles != null && !pourParticles.isPlaying) pourParticles.Play();

            // 1フレームあたりに注ぐ量を計算
            float amountToPour = pourRatePerSecond * Time.deltaTime;
            
            // フラスコの残りが少なければ、残り全部を注ぐ
            if (FlaskReceiver.Instance.currentLiquidAmount < amountToPour) 
            {
                amountToPour = FlaskReceiver.Instance.currentLiquidAmount;
            }
            
            // フラスコの中身を減らす（LateUpdateで勝手に液面が下がります）
            FlaskReceiver.Instance.currentLiquidAmount -= amountToPour;
            
            // ピペットに中身を増やす
            nearestPipette.ReceiveLiquid(amountToPour, FlaskReceiver.Instance.completedResult);

            // フラスコが空っぽになったら完全リセット！
            if (FlaskReceiver.Instance.currentLiquidAmount <= 0f)
            {
                FlaskReceiver.Instance.EmptyFlask();
                if (pourParticles != null) pourParticles.Stop();
            }
            // ---------------------------------
        }
        else
        {
            // 遠い時、またはピペットが満タンの時は注ぐのをやめる
            flaskRoot.position = Vector3.Lerp(flaskRoot.position, targetPos, Time.deltaTime * 15f);
            flaskPivot.localRotation = Quaternion.Slerp(flaskPivot.localRotation, initialRotation, Time.deltaTime * tiltSpeed);
            if (pourParticles != null && pourParticles.isPlaying) pourParticles.Stop();
        }
    }

    private void ReturnToDefault()
    {
        flaskRoot.position = Vector3.Lerp(flaskRoot.position, initialPosition, Time.deltaTime * 10f);
        velX = 0f; velZ = 0f;
        angleX = Mathf.Lerp(angleX, 0, Time.deltaTime * 15f);
        angleZ = Mathf.Lerp(angleZ, 0, Time.deltaTime * 15f);
        flaskPivot.localRotation = initialRotation * Quaternion.Euler(angleX, 0, angleZ);
        if (pourParticles != null && pourParticles.isPlaying) pourParticles.Stop();
    }
}