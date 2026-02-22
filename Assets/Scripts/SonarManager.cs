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

    [Tooltip("回転させる外周のコンパス画像")]
    public RectTransform compassRing;

    [Tooltip("向きをまっすぐに固定する文字のTransformを入れてください")]
    public Transform[] compassLabels;

    [Header("Information Display")]
    public SubmarineStatus subStatus;         
    public TMPro.TMP_Text statusDisplayText;



    
    public GameObject blipPrefab;

    [Header("Sonar Settings")]
    public float rotationSpeed = 180f;
    public Transform player;
    public float sonarRange = 50f;
    private float radarUIRadius;
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
            // ==========================================
            // ターゲットの種類に応じて光点の色を変える
            // ==========================================
            Image blipImage = newBlip.GetComponent<Image>();
            TMPro.TextMeshProUGUI blipText = newBlip.GetComponentInChildren<TMPro.TextMeshProUGUI>(); 

            if (blipImage != null)
            {
                // まず最初に、Textオブジェクトを強制的に「非表示（OFF）」にしておく
                if (blipText != null) blipText.gameObject.SetActive(false);

                switch (target.targetType)
                {
                    case SubmarineTargetType.Mine:
                        blipImage.color = Color.red; 
                        // ※もうTextを空にする処理（blipText.text = "";）は不要なので消してOKです！
                        break;
                    case SubmarineTargetType.HostileBio:
                        blipImage.color = new Color(1f, 0.4f, 0f); 
                        break;
                    case SubmarineTargetType.NeutralBio:
                        blipImage.color = Color.cyan; 
                        break;
                    case SubmarineTargetType.Item:
                        blipImage.color = Color.yellow; 
                        break;
                    
                    case SubmarineTargetType.Objective:
                        blipImage.color = new Color(0f, 1f, 0f, 0.4f); 

                        float uiSize = (target.areaRadius / sonarRange) * radarUIRadius * 2f;
                        uiSize = Mathf.Max(uiSize, 30f); 
                        
                        RectTransform blipRt = newBlip.GetComponent<RectTransform>();
                        blipRt.sizeDelta = new Vector2(uiSize, uiSize); 

                        // Objectiveであり、かつ targetLabel に文字が入力されている時だけONにする！
                        if (blipText != null && !string.IsNullOrEmpty(target.targetLabel))
                        {
                            blipText.gameObject.SetActive(true); // ここでON！
                            blipText.text = target.targetLabel;
                            blipText.color = Color.green; 
                        }
                        break;
                }
            }

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

        RectTransform rt = sonarDisplayImage.GetComponent<RectTransform>();
        // Widthの半分をレーダーの半径とする
        radarUIRadius = rt.rect.width / 2f;
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

        // ==========================================
        // コンパスリングの回転（プレイヤーと連動）
        // ==========================================
        if (compassRing != null)
        {
            
            compassRing.localEulerAngles = new Vector3(0, 0, player.eulerAngles.y);
            // 文字だけを「常に上向き」に固定（逆回転）する
            if (compassLabels != null)
            {
                foreach (Transform label in compassLabels)
                {
                    // World Spaceで常に角度を0（真っ直ぐ）に保つ最強の1行
                    label.eulerAngles = Vector3.zero;
                }
            }
        }

        // ==========================================
        // 情報ディスプレイの更新
        // ==========================================
        if (subStatus != null && statusDisplayText != null)
        {
            // プレイヤーのY軸を「方角（0〜360度）」として綺麗に表示する計算
            float heading = Mathf.Repeat(player.eulerAngles.y, 360f);

            // ★追加：SubmarineControllerから現在のギア名を取得する
            string currentGearName = "UNKNOWN";
            SubmarineController subController = player.GetComponent<SubmarineController>();
            if (subController != null)
            {
                currentGearName = subController.gears[subController.currentGearIndex].gearName;
            }

            // ★修正：GEAR の表示行を追加！
            statusDisplayText.text = 
                $"HULL INTEG : {subStatus.currentHP:F0} / {subStatus.maxHP:F0}\n\n" +
                $"GEAR       : {currentGearName}\n" +
                $"SPEED      : {subStatus.currentSpeed:F1} KTS\n" +
                $"TURN RATE  : {subStatus.currentTurnRate:F1} DEG/S\n" +
                $"HEADING    : {heading:F0}°";
        }

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
            // ターゲットが破壊（機雷が爆発など）されていたら、光点UIも消して次へ
            if (targets[i] == null || !targets[i].enabled || !targets[i].gameObject.activeInHierarchy)
            {
                if (blips[i] != null && blips[i].activeSelf) blips[i].SetActive(false);
                continue; // 処理を飛ばして次のターゲットへ
            }

            Vector3 relativePos = targets[i].transform.position - player.position;
            float distance = new Vector2(relativePos.x, relativePos.z).magnitude;

            if (distance <= sonarRange)
            {
                // プレイヤーから見たターゲットの角度
                float angle = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
                angle -= player.eulerAngles.y;

                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(-currentSweepAngle, angle));

                UnityEngine.UI.Image blipImage = blips[i].GetComponent<UnityEngine.UI.Image>();

                // ★通過判定：走査線がターゲットの5度以内を通過したら「Ping（点灯）」！
                if (angleDiff < 5f) 
                {
                    blips[i].SetActive(true);
                    
                    if (blipImage != null)
                    {
                        Color c = blipImage.color;
                        c.a = 1f; // アルファ値（透明度）を100%にして強く光らせる
                        blipImage.color = c;
                    }

                    float distanceRatio = distance / sonarRange;
                    float angleRad = angle * Mathf.Deg2Rad;
                    float uiX = Mathf.Sin(angleRad) * distanceRatio * radarUIRadius;
                    float uiY = Mathf.Cos(angleRad) * distanceRatio * radarUIRadius;
                    blips[i].transform.localPosition = new Vector3(uiX, uiY, 0);
                }

                // ★フェードアウト処理：光っている間だけ実行
                if (blips[i].activeSelf && blipImage != null)
                {
                    Color c = blipImage.color;
                    // 毎フレーム少しずつ透明にする（0.5fなら約2秒かけてスッと消える）
                    c.a -= Time.deltaTime * 0.5f; 
                    blipImage.color = c;

                    // 完全に透明になったら処理節約のためにUIをオフにする
                    if (c.a <= 0f)
                    {
                        blips[i].SetActive(false);
                    }

                }
            }
            else
            {
                blips[i].SetActive(false); // ソナー範囲外ならオフ
            }
        }
    }
}