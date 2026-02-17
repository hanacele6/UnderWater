using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SonarManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform sweepLine;
    public Transform radarCenter;
    [Tooltip("今回追加：ピクセルを描画する透明なキャンバス")]
    public RawImage sonarDisplayImage; 
    
    public GameObject blipPrefab;

    [Header("Sonar Settings")]
    public float rotationSpeed = 180f;
    public Transform player;
    public float sonarRange = 50f;
    public float radarUIRadius = 300f;
    public LayerMask wallLayer; 

    [Header("Echo Visuals (Texture)")]
    [Tooltip("生成するテクスチャの解像度（512推奨。大きすぎると重くなります）")]
    public int textureSize = 512;
    [Tooltip("壁(R0)から端(R)までのグラデーション")]
    public Gradient shadowGradient;
    [Tooltip("描画の隙間を埋めるための解像度")]
    public float scanResolution = 0.5f;
    [Tooltip("1フレームあたりのフェードアウト量（0〜255）")]
    public byte fadeSpeed = 3;

    private List<SonarTarget> targets = new List<SonarTarget>();
    private List<GameObject> blips = new List<GameObject>();
    private float currentSweepAngle = 0f; 

    // テクスチャ操作用の変数
    private Texture2D sonarTexture;
    private Color32[] pixelBuffer; // Color32を使うことでCPUの処理を爆速にします
    private int centerPixel;

    void Start()
    {
        // ターゲット準備
        SonarTarget[] foundTargets = FindObjectsOfType<SonarTarget>();
        foreach (SonarTarget target in foundTargets)
        {
            targets.Add(target);
            GameObject newBlip = Instantiate(blipPrefab, radarCenter);
            newBlip.SetActive(false);
            blips.Add(newBlip);
        }

        // --- キャンバス（テクスチャ）の準備 ---
        sonarTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        pixelBuffer = new Color32[textureSize * textureSize];
        
        // 全ピクセルを透明で初期化
        Color32 clearColor = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixelBuffer.Length; i++) pixelBuffer[i] = clearColor;
        
        sonarTexture.SetPixels32(pixelBuffer);
        sonarTexture.Apply();
        sonarDisplayImage.texture = sonarTexture;
        
        centerPixel = textureSize / 2;
    }

    void Update()
    {
        if (player == null || sweepLine == null) return;

        float angleDelta = rotationSpeed * Time.deltaTime;

        // ==========================================
        // 0. 全体のピクセルを少しずつ透明にする（フェードアウト）
        // ==========================================
        FadeOutPixels();

        // ==========================================
        // 1. 地形（壁）のエコーを描画（R0 < R の塗りつぶし）
        // ==========================================
        int raysToShoot = Mathf.Max(1, Mathf.CeilToInt(angleDelta / scanResolution));

        for (int r = 0; r < raysToShoot; r++)
        {
            float fractionalAngle = currentSweepAngle - (angleDelta * ((float)r / raysToShoot));
            if (fractionalAngle <= -360f) fractionalAngle += 360f;

            float horizontalRad = -fractionalAngle * Mathf.Deg2Rad;
            
            // プレイヤーの向きを考慮したRayの方向
            Vector3 direction = player.rotation * new Vector3(Mathf.Sin(horizontalRad), 0, Mathf.Cos(horizontalRad));
            Vector3 rayOrigin = player.position + new Vector3(0, 1.0f, 0);

            if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, sonarRange, wallLayer))
            {
                // R0: 最初に障害物に当たった距離（ピクセル単位）
                float r0 = (hit.distance / sonarRange) * centerPixel;
                // R: レーダーの最大半径（ピクセル単位）
                float maxR = centerPixel;

                // R0 から R まで直線を引くようにピクセルを塗る
                // （隙間ができないように 0.5 ピクセル刻みでループ）
                for (float radius = r0; radius < maxR; radius += 0.5f)
                {
                    // R0が0、最大Rが1になる割合（グラデーション用）
                    float t = (radius - r0) / (maxR - r0);
                    Color32 drawColor = shadowGradient.Evaluate(t);

                    // X, Y座標の計算
                    int px = Mathf.RoundToInt(centerPixel + Mathf.Sin(horizontalRad) * radius);
                    int py = Mathf.RoundToInt(centerPixel + Mathf.Cos(horizontalRad) * radius);

                    // テクスチャの範囲内に収まっているかチェックして塗る
                    if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                    {
                        pixelBuffer[py * textureSize + px] = drawColor;
                    }
                }
            }
        }

        // ピクセルの変更をテクスチャに適用
        sonarTexture.SetPixels32(pixelBuffer);
        sonarTexture.Apply();

        // ==========================================
        // 2. 走査線の更新 & 3. 敵・アイテム処理（変更なし）
        // ==========================================
        currentSweepAngle -= angleDelta;
        if (currentSweepAngle <= -360f) currentSweepAngle += 360f;
        sweepLine.localEulerAngles = new Vector3(0, 0, currentSweepAngle);

        UpdateBlips();
    }

    // ==========================================
    // 補助メソッド
    // ==========================================
    private void FadeOutPixels()
    {
        for (int i = 0; i < pixelBuffer.Length; i++)
        {
            if (pixelBuffer[i].a > 0)
            {
                // アルファ値を fadeSpeed 分減らす（0未満にはしない）
                pixelBuffer[i].a = (byte)Mathf.Max(0, pixelBuffer[i].a - fadeSpeed);
            }
        }
    }

    private void UpdateBlips()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            Vector3 relativePos = targets[i].transform.position - player.position;
            float distance = new Vector2(relativePos.x, relativePos.z).magnitude;

            if (distance <= sonarRange)
            {
                blips[i].SetActive(true);
                float distanceRatio = distance / sonarRange;
                float angle = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
                angle -= player.eulerAngles.y;

                float angleRad = angle * Mathf.Deg2Rad;
                float uiX = Mathf.Sin(angleRad) * distanceRatio * radarUIRadius;
                float uiY = Mathf.Cos(angleRad) * distanceRatio * radarUIRadius;

                blips[i].transform.localPosition = new Vector3(uiX, uiY, 0);
            }
            else
            {
                blips[i].SetActive(false);
            }
        }
    }
}