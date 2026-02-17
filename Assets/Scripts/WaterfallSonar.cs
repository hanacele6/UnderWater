using UnityEngine;
using UnityEngine.UI;

public class WaterfallSonar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("描画先のRawImage")]
    public RawImage displayImage;

    [Header("Sonar Settings")]
    public Transform player;
    [Tooltip("前方スキャンする角度の幅（例：正面を中心に90度）")]
    public float scanAngle = 90f;
    [Tooltip("ソナーの届く最大距離")]
    public float maxDistance = 50f;
    [Tooltip("地形として判定するレイヤー")]
    public LayerMask terrainLayer;
    
    [Header("Display Resolution")]
    [Tooltip("横方向のRayの数（解像度）")]
    public int resolutionX = 128;
    [Tooltip("縦方向の履歴の長さ（スクロール量）")]
    public int resolutionY = 256;
    [Tooltip("何秒に1回スキャンを更新するか")]
    public float scanInterval = 0.05f;

    [Header("Visuals")]
    [Tooltip("距離（または高さ）に応じた色の変化")]
    public Gradient depthColor;
    [Tooltip("反応がなかった場所（深海）の色")]
    public Color backgroundColor = Color.black;

    private Texture2D texture;
    private Color[] pixelBuffer; // ピクセルデータを保持する1次元配列
    private float timer;

    void Start()
    {
        // 1. 動的にテクスチャを生成し、RawImageにセット
        texture = new Texture2D(resolutionX, resolutionY, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear; // 少しぼかして滑らかにする
        displayImage.texture = texture;

        // 2. ピクセル配列の初期化
        pixelBuffer = new Color[resolutionX * resolutionY];
        for (int i = 0; i < pixelBuffer.Length; i++)
        {
            pixelBuffer[i] = backgroundColor;
        }
        
        // 初期状態を適用
        texture.SetPixels(pixelBuffer);
        texture.Apply();
    }

    void Update()
    {
        if (player == null) return;

        timer += Time.deltaTime;
        if (timer >= scanInterval)
        {
            timer = 0f;
            ScanAndScroll();
        }
    }

    void ScanAndScroll()
    {
        // ==========================================
        // 1. 過去のデータを1行分「下」にズラす（超高速処理）
        // ==========================================
        // for文を回すのではなく、C#標準のメモリコピーを使うことでCPU負荷を極限まで下げます
        System.Array.Copy(pixelBuffer, resolutionX, pixelBuffer, 0, resolutionX * (resolutionY - 1));

        // ==========================================
        // 2. 最新のスキャン結果を「一番上の行」に書き込む
        // ==========================================
        int topRowStartIndex = resolutionX * (resolutionY - 1);

        for (int x = 0; x < resolutionX; x++)
        {
            // 左端から右端まで、Rayを飛ばす角度を計算
            float normalizedX = (float)x / (resolutionX - 1);
            float currentAngle = Mathf.Lerp(-scanAngle / 2f, scanAngle / 2f, normalizedX);
            
            // プレイヤーの向きを基準に、Rayの方向ベクトルを作成
            Vector3 direction = player.rotation * Quaternion.Euler(0, currentAngle, 0) * Vector3.forward;
            
            Color hitColor = backgroundColor;

            // 前方に向かってRayを発射
            if (Physics.Raycast(player.position, direction, out RaycastHit hit, maxDistance, terrainLayer))
            {
                // 近いほど1、遠いほど0になる割合
                float distanceRatio = 1f - (hit.distance / maxDistance);
                hitColor = depthColor.Evaluate(distanceRatio);
            }

            // 配列の一番上の行に色データを格納
            pixelBuffer[topRowStartIndex + x] = hitColor;
        }

        // ==========================================
        // 3. テクスチャに変更を適用（GPUへ転送）
        // ==========================================
        texture.SetPixels(pixelBuffer);
        texture.Apply();
    }
}