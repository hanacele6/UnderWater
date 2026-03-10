using UnityEngine;

[RequireComponent(typeof(InteractableHighlight))] 
public class MissionGuide : MonoBehaviour
{
    public static MissionGuide Instance;

    [Header("動きの設定")]
    public float heightOffset = 2.0f;  // 対象の頭上に浮かせる高さ
    public float bobbingSpeed = 2.0f;  // 上下運動のスピード
    public float bobbingAmount = 0.2f; // 上下運動の幅

    [Header("非表示設定")]
    [Tooltip("プレイヤーがこの距離(m)より近づいたらマーカーを隠す")]
    public float hideDistance = 3.0f;

    private Transform currentTarget;
    private Renderer meshRenderer; // 3D図形の描画コンポーネント
    private InteractableHighlight highlight; // 光らせるスクリプト
    private Transform playerTransform;

    void Awake()
    {
        Instance = this;

        // コンポーネントを取得
        meshRenderer = GetComponent<Renderer>();
        highlight = GetComponent<InteractableHighlight>();

        // 最初は隠しておく
        if (meshRenderer != null) meshRenderer.enabled = false;
        if (highlight != null) highlight.enabled = false;

        // プレイヤーを探して記憶しておく（タグが"Player"になっていること！）
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
        UpdateVisibility(); // ターゲットが変わった瞬間に表示判定を行う
    }

    void LateUpdate()
    {
        if (currentTarget == null) return;

        // 1. フワフワ移動
        float newY = currentTarget.position.y + heightOffset + (Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount);
        transform.position = new Vector3(currentTarget.position.x, newY, currentTarget.position.z);

        // 2. kurukurumawasu
        if (Camera.main != null)
        {
           transform.Rotate(0f, 45f * Time.deltaTime, 0f);
        }

        // 3. プレイヤーとの距離を計算して表示/非表示を切り替える
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (currentTarget == null || playerTransform == null || meshRenderer == null)
        {
            if (meshRenderer != null) meshRenderer.enabled = false;
            if (highlight != null) highlight.enabled = false;
            return;
        }

        // プレイヤーとマーカーの距離を測る
        float distance = Vector3.Distance(playerTransform.position, transform.position);

        if (distance <= hideDistance)
        {
            // ★指定した距離より近づいた！ → 図形も光もフッと消す
            meshRenderer.enabled = false;
            highlight.enabled = false;
        }
        else
        {
            // 離れている！ → 図形を表示して、ピカピカに光らせる
            meshRenderer.enabled = true;
            
            // 強制的にハイライトをONにする
            highlight.OutlineMode = InteractableHighlight.Mode.OutlineAll;
            highlight.enabled = true; 
        }
    }
}