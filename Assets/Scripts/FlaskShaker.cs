using UnityEngine;

public class FlaskShaker : MonoBehaviour
{
    [Header("フラスコ設定")]
    public Transform flaskPivot; 
    public Camera deskCamera;

    [Header("持ち上げ＆揺れ設定")]
    public float swingSensitivity = 0.5f; 
    public float swingDamping = 10f;      
    public float maxTiltAngle = 60f;

    [Header("調合の進行設定")]
    // 💡 ピクセル計算に戻したので、数値を小さく戻しました
    public float progressPerShake = 0.05f; 

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    
    private float zCoord;
    private Vector3 offset;
    private bool isDragging = false;

    private Vector3 lastMousePos;
    private Vector2 currentVelocity;

    void Start()
    {
        if (flaskPivot != null)
        {
            initialPosition = flaskPivot.position;
            initialRotation = flaskPivot.localRotation;
        }
        if (deskCamera == null) deskCamera = GameObject.Find("DeskCamera")?.GetComponent<Camera>();
    }

    private Vector3 GetMouseAsWorldPoint()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord;
        return deskCamera.ScreenToWorldPoint(mousePoint);
    }

    void Update()
    {
        if (flaskPivot == null || deskCamera == null) return;

        // 1. クリックした瞬間
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = deskCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(flaskPivot))
                {
                    isDragging = true;
                    lastMousePos = Input.mousePosition;
                    
                    zCoord = deskCamera.WorldToScreenPoint(flaskPivot.position).z;
                    offset = flaskPivot.position - GetMouseAsWorldPoint();

                    // 💡 FPS低下防止：ドラッグ中は物理演算を一時停止する
                    Rigidbody rb = flaskPivot.GetComponentInChildren<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;

                    if (FlaskReceiver.Instance != null && FlaskReceiver.Instance.IsFull && FlaskReceiver.Instance.addedItems.Count > 0)
                    {
                        FlaskReceiver.Instance.SetProgressBarVisible(true);
                    }
                }
            }
        }

        // 2. ドラッグ中
        if (Input.GetMouseButton(0) && isDragging)
        {
            // 位置を完璧に追従
            Vector3 targetPosition = GetMouseAsWorldPoint() + offset;
            flaskPivot.position = Vector3.Lerp(flaskPivot.position, targetPosition, Time.deltaTime * 30f);

            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - lastMousePos;
            lastMousePos = currentMousePos;

            float shakeForce = mouseDelta.magnitude;
            if (shakeForce > 0.1f && FlaskReceiver.Instance != null)
            {
                FlaskReceiver.Instance.AddMixProgress(shakeForce * progressPerShake);
            }

            currentVelocity = Vector2.Lerp(currentVelocity, new Vector2(mouseDelta.x, mouseDelta.y), Time.deltaTime * 15f);
            float targetTiltX = currentVelocity.y * swingSensitivity; 
            float targetTiltZ = -currentVelocity.x * swingSensitivity; 

            targetTiltX = Mathf.Clamp(targetTiltX, -maxTiltAngle, maxTiltAngle);
            targetTiltZ = Mathf.Clamp(targetTiltZ, -maxTiltAngle, maxTiltAngle);

            Quaternion targetRotation = initialRotation * Quaternion.Euler(targetTiltX, 0, targetTiltZ);
            flaskPivot.localRotation = Quaternion.Lerp(flaskPivot.localRotation, targetRotation, Time.deltaTime * swingDamping);
        }

        // 3. 手を離した時
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

            // 物理演算を元に戻す
            Rigidbody rb = flaskPivot.GetComponentInChildren<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            if (FlaskReceiver.Instance != null) FlaskReceiver.Instance.SetProgressBarVisible(false);
        }

        if (!isDragging)
        {
            flaskPivot.position = Vector3.Lerp(flaskPivot.position, initialPosition, Time.deltaTime * 10f);
            flaskPivot.localRotation = Quaternion.Lerp(flaskPivot.localRotation, initialRotation, Time.deltaTime * 10f);
        }
    }
}