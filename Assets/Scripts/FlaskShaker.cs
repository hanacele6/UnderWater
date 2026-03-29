using UnityEngine;
using System.Collections.Generic;

public class FlaskShaker : MonoBehaviour
{
    [Header("階層の参照（設定必須）")]
    [Tooltip("一番上の親（FlaskRoot）をアタッチ")]
    public Transform flaskRoot;  
    [Tooltip("中間の空オブジェクト（FlaskPivot）をアタッチ")]
    public Transform flaskPivot; 
    public Camera deskCamera;

    [Header("ふりこの物理設定")]
    public float swingSensitivity = 20f; 
    public float gravity = 150f;
    public float damping = 5f; 
    public float maxTiltAngle = 60f;

    [Header("調合プログレス設定")]
    public float progressPerShake = 2.0f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isDragging = false;
    private float zDistance;

    // ふりこ計算用の変数
    private float angleX = 0f;
    private float angleZ = 0f;
    private float angularVelocityX = 0f;
    private float angularVelocityZ = 0f;

    private MonoBehaviour highlightScript;

    // FPS低下防止用のコライダー退避リスト
    private List<Collider> disabledColliders = new List<Collider>();

    void Start()
    {
        if (flaskRoot != null) initialPosition = flaskRoot.position;
        if (flaskPivot != null) initialRotation = flaskPivot.localRotation;
        if (deskCamera == null) deskCamera = GameObject.Find("DeskCamera")?.GetComponent<Camera>();
        highlightScript = flaskRoot.GetComponentInChildren<InteractableHighlight>();
    }

    void Update()
    {
        HandleInput();
        if (isDragging) PerformDrag();
        else ReturnToDefault();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject() == true) return;

            Ray ray = deskCamera.ScreenPointToRay(Input.mousePosition);

            // IsTrigger（素材投入口）は無視して本体だけを判定
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                // Root以下のどこをクリックしても反応する
                if (hit.transform == transform || hit.transform.IsChildOf(flaskRoot))
                {
                    isDragging = true;
                    if (highlightScript != null) 
                    {
                        ((InteractableHighlight)highlightScript).isSuppressed = true;
                        ((InteractableHighlight)highlightScript).ChangeHighlightState(InteractableHighlight.HighlightState.None);
                    }

                    // クリックした瞬間に、RootがマウスのZ深度にスナップ
                    zDistance = deskCamera.WorldToScreenPoint(flaskRoot.position).z;

                    // 💡 FPS低下の完全排除：動くことで重くなる元凶（MeshCollider）をオフにする
                    Collider[] colliders = flaskRoot.GetComponentsInChildren<Collider>();
                    disabledColliders.Clear();
                    foreach (Collider col in colliders)
                    {
                        if (!col.isTrigger) // 素材投入用のTriggerは残す
                        {
                            col.enabled = false;
                            disabledColliders.Add(col);
                        }
                    }

                    Rigidbody rb = flaskRoot.GetComponentInChildren<Rigidbody>();
                    if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }

                    if (FlaskReceiver.Instance != null && FlaskReceiver.Instance.IsFull)
                        FlaskReceiver.Instance.SetProgressBarVisible(true);
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            if (highlightScript != null) 
            {
                ((InteractableHighlight)highlightScript).isSuppressed = false;
            }

            // 消していたコライダーを復活させる
            foreach (Collider col in disabledColliders)
            {
                if (col != null) col.enabled = true;
            }
            disabledColliders.Clear();

            Rigidbody rb = flaskRoot.GetComponentInChildren<Rigidbody>();
            if (rb != null) { rb.isKinematic = false; rb.detectCollisions = true; }

            if (FlaskReceiver.Instance != null) FlaskReceiver.Instance.SetProgressBarVisible(false);
        }
    }

    private void PerformDrag()
    {
        // 1. Root（親）の位置をカーソルに完全同期させる（絶対にカーソルから離れない）
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = zDistance;
        flaskRoot.position = deskCamera.ScreenToWorldPoint(screenPos);

        // 2. マウスの物理的な移動量を取得
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if ((Mathf.Abs(mouseX) > 0.1f || Mathf.Abs(mouseY) > 0.1f) && FlaskReceiver.Instance != null)
        {
            FlaskReceiver.Instance.AddMixProgress((Mathf.Abs(mouseX) + Mathf.Abs(mouseY)) * progressPerShake);
        }

        // 3. Pivot（子）だけを回転させる（本物のふりこ物理演算）
        angularVelocityX += mouseY * swingSensitivity; // 縦に振ると前後に傾く
        angularVelocityZ -= mouseX * swingSensitivity; // 横に振ると左右に傾く

        // 重力（戻ろうとする力）
        angularVelocityX -= angleX * gravity * Time.deltaTime;
        angularVelocityZ -= angleZ * gravity * Time.deltaTime;

        // 空気抵抗（揺れの収束）
        float dragFactor = Mathf.Clamp01(1f - damping * Time.deltaTime);
        angularVelocityX *= dragFactor;
        angularVelocityZ *= dragFactor;

        // 角度の更新
        angleX += angularVelocityX * Time.deltaTime;
        angleZ += angularVelocityZ * Time.deltaTime;

        angleX = Mathf.Clamp(angleX, -maxTiltAngle, maxTiltAngle);
        angleZ = Mathf.Clamp(angleZ, -maxTiltAngle, maxTiltAngle);

        // Rootは動かさず、Pivotだけを傾ける
        flaskPivot.localRotation = initialRotation * Quaternion.Euler(angleX, 0, angleZ);
    }

    private void ReturnToDefault()
    {
        if (flaskRoot == null || flaskPivot == null) return;
        
        // 1. Root（位置）はスムーズに元の机の上へ戻る
        flaskRoot.position = Vector3.Lerp(flaskRoot.position, initialPosition, Time.deltaTime * 10f);
        
        // 2. 💡 修正：机に置いた後は「慣性（勢い）」を強制的にゼロにしてピタッと止める
        angularVelocityX = 0f;
        angularVelocityZ = 0f;
        
        // 3. 角度をスムーズに「0度（真っ直ぐ）」へ戻す
        angleX = Mathf.Lerp(angleX, 0f, Time.deltaTime * 15f);
        angleZ = Mathf.Lerp(angleZ, 0f, Time.deltaTime * 15f);

        flaskPivot.localRotation = initialRotation * Quaternion.Euler(angleX, 0, angleZ); 
    }
}