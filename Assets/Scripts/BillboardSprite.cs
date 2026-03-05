using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // エディタ機能を呼び出す
#endif

[ExecuteAlways] // エディタ上でも動かす魔法
public class BillboardSprite : MonoBehaviour
{
    [Header("4方向の画像を設定")]
    public Sprite frontSprite;
    public Sprite backSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    private SpriteRenderer sr;
    private Transform targetCamera;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        // 1. カメラの取得ロジック
        if (Application.isPlaying)
        {
            // ゲーム中
            if (Camera.main != null) targetCamera = Camera.main.transform;
        }
        else
        {
            // 編集中 (Sceneビュー)
#if UNITY_EDITOR
            // 今アクティブなSceneビューを探す
            if (SceneView.lastActiveSceneView != null)
            {
                targetCamera = SceneView.lastActiveSceneView.camera.transform;
            }

            // エディタに「寝るな！毎フレーム更新しろ！」と命令を送る
            // これがないと、カメラを動かしてもビルボードがカクついたり止まったりします
            EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        // カメラが見つからなければ何もしない
        if (targetCamera == null || sr == null) return;

        // 2. ビルボード処理（Y軸回転のみ）
        // ターゲットの方向を向く（高さは無視）
        Vector3 targetPos = targetCamera.position;
        targetPos.y = transform.position.y;
        transform.LookAt(targetPos);

        // 3. 画像切り替え処理
        UpdateSpriteDirection();
    }

    void UpdateSpriteDirection()
    {
        // 親（移動方向）の基準。親がいなければ自分自身
        Transform root = transform.parent != null ? transform.parent : transform;
        
        // カメラへの方向ベクトル
        Vector3 toCam = targetCamera.position - root.position;
        toCam.y = 0; // 高さは無視

        // 親の正面(root.forward)と、カメラ方向(toCam)の角度差を計算
        float angle = Vector3.SignedAngle(root.forward, toCam, Vector3.up);

        // 角度に応じてスプライトを差し替え
        if (angle > -45f && angle <= 45f)
        {
            sr.sprite = frontSprite;
        }
        else if (angle > 45f && angle <= 135f)
        {
            sr.sprite = rightSprite;
        }
        else if (angle > -135f && angle <= -45f)
        {
            sr.sprite = leftSprite;
        }
        else
        {
            sr.sprite = backSprite;
        }
    }
}