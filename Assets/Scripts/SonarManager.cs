using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SonarManager : MonoBehaviour
{
    // ★追加：どこからでもアクセスできるようにする
    public static SonarManager Instance;

    [Header("UI References")]
    public Transform sweepLine;
    public Transform radarCenter;
    [Tooltip("ピクセルを描画する透明なキャンバス")]
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
    public int textureSize = 512;
    public Gradient shadowGradient;
    public float scanResolution = 0.5f;
    public byte fadeSpeed = 3;

    [Header("Mission Navigation & Visual Pulse")]
    [Tooltip("レーダーの外周を回る目的地のマーカー（矢印など）")]
    public RectTransform missionMarker;
    
    [Tooltip("波紋として広がるリング状の画像（UI）")]
    public RectTransform pulseRing;
    [Tooltip("波紋が広がるスピード")]
    public float pulseExpandSpeed = 150f;
    [Tooltip("波紋が透明になって消えるスピード")]
    public float pulseFadeSpeed = 1.0f;

    [Tooltip("この距離(m)より近づいたら波紋を出し始める")]
    public float pulseTriggerDistance = 30f;
    
    // (※音を鳴らさない場合はAudioSource等は消してもOKですが、一応残してあります)
    public AudioSource sonarAudioSource;
    public AudioClip pulseSound;
    
    private Transform currentMissionTarget;
    private float pulseTimer = 0f;
    private float pulseInterval = 1.5f; 
    private float currentPulseAlpha = 0f; // 波紋の透明度を管理

    private List<SonarTarget> targets = new List<SonarTarget>();
    private List<GameObject> blips = new List<GameObject>();
    private float currentSweepAngle = 0f; 

    private Texture2D sonarTexture;
    private Color32[] pixelBuffer; 
    private int centerPixel;

    void Awake()
    {
        // ★追加：Instanceの設定
        Instance = this;
    }

    void Start()
    {
        // ターゲット準備 (既存コードそのまま)
        SonarTarget[] foundTargets = FindObjectsOfType<SonarTarget>();
        foreach (SonarTarget target in foundTargets)
        {
            targets.Add(target);
            GameObject newBlip = Instantiate(blipPrefab, radarCenter);
            newBlip.SetActive(false);

            Image blipImage = newBlip.GetComponent<Image>();
            TMPro.TextMeshProUGUI blipText = newBlip.GetComponentInChildren<TMPro.TextMeshProUGUI>(); 

            if (blipImage != null)
            {
                if (blipText != null) blipText.gameObject.SetActive(false);

                switch (target.targetType)
                {
                    case SubmarineTargetType.Mine:
                        blipImage.color = Color.red; 
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

                        if (blipText != null && !string.IsNullOrEmpty(target.targetLabel))
                        {
                            blipText.gameObject.SetActive(true);
                            blipText.text = target.targetLabel;
                            blipText.color = Color.green; 
                        }
                        break;
                }
            }
            blips.Add(newBlip);
        }

        // キャンバス（テクスチャ）の準備 (既存コードそのまま)
        sonarTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        pixelBuffer = new Color32[textureSize * textureSize];
        
        Color32 clearColor = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixelBuffer.Length; i++) pixelBuffer[i] = clearColor;
        
        sonarTexture.SetPixels32(pixelBuffer);
        sonarTexture.Apply();
        sonarDisplayImage.texture = sonarTexture;
        
        centerPixel = textureSize / 2;

        RectTransform rt = sonarDisplayImage.GetComponent<RectTransform>();
        radarUIRadius = rt.rect.width / 2f;

        if (missionMarker != null) missionMarker.gameObject.SetActive(false);

        currentPulseAlpha = 0f;
        if (pulseRing != null)
        {
            UnityEngine.UI.Image ringImage = pulseRing.GetComponent<UnityEngine.UI.Image>();
            if (ringImage != null)
            {
                Color c = ringImage.color;
                c.a = 0f;
                ringImage.color = c;
            }
        }
    }

    // ★追加：外部から目的地をセットするメソッド
    public void SetMissionTarget(Transform target)
    {
        currentMissionTarget = target;
        if (missionMarker != null)
        {
            missionMarker.gameObject.SetActive(target != null);
        }

        // ★追加：ターゲットが無い場合は、波紋を強制的に透明にして消す
        if (target == null)
        {
            currentPulseAlpha = 0f;
            if (pulseRing != null)
            {
                UnityEngine.UI.Image ringImage = pulseRing.GetComponent<UnityEngine.UI.Image>();
                if (ringImage != null)
                {
                    Color c = ringImage.color;
                    c.a = 0f;
                    ringImage.color = c;
                }
            }
        }
    }

    void Update()
    {
        if (player == null || sweepLine == null) return;

        float angleDelta = rotationSpeed * Time.deltaTime;

        // 0〜3. 既存のエコー描画・走査線・光点更新（変更なし）
        FadeOutPixels();
        DrawEchoes(angleDelta); // ※Updateの中身が長くなるのでメソッドに分けました（下部に記述）
        
        currentSweepAngle -= angleDelta;
        if (currentSweepAngle <= -360f) currentSweepAngle += 360f;
        sweepLine.localEulerAngles = new Vector3(0, 0, currentSweepAngle);

        UpdateBlips();

        // コンパスリングの回転（変更なし）
        if (compassRing != null)
        {
            compassRing.localEulerAngles = new Vector3(0, 0, player.eulerAngles.y);
            if (compassLabels != null)
            {
                foreach (Transform label in compassLabels) label.eulerAngles = Vector3.zero;
            }
        }

        // 情報ディスプレイの更新（変更なし）
        UpdateStatusDisplay();

        // ==========================================
        // ★追加：ナビゲーションと接近パルスの処理
        // ==========================================
        UpdateMissionNavigation();
    }

    // ==========================================
    // 修正版：ナビゲーションと波紋アニメーション処理
    // ==========================================
    private void UpdateMissionNavigation()
    {
        if (currentMissionTarget == null || missionMarker == null) return;

        // 1. 目的地への方角を計算（プレイヤーの向きを基準にする）
        Vector3 dir = currentMissionTarget.position - player.position;
        float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float relativeAngle = targetAngle - player.eulerAngles.y;
        float angleRad = relativeAngle * Mathf.Deg2Rad;

        // ==========================================
        // ★修正1：マーカーを「レーダーの縁（円周上）」に移動させる
        // ==========================================
        float markerX = Mathf.Sin(angleRad) * radarUIRadius;
        float markerY = Mathf.Cos(angleRad) * radarUIRadius;
        
        // 中心から指定した半径（レーダーの縁）の座標へセット
        missionMarker.localPosition = new Vector3(markerX, markerY, 0);
        // マーカーの画像が外側（またはターゲット方向）を向くように回転
        missionMarker.localEulerAngles = new Vector3(0, 0, -relativeAngle);

        // 2. 距離を測って、近づいていたら波紋を発生させる
        float distance = new Vector2(dir.x, dir.z).magnitude;
        
        if (distance <= pulseTriggerDistance)
        {
            pulseTimer += Time.deltaTime;
            // 距離が近いほど波紋の発生間隔が早くなる
            float currentInterval = Mathf.Lerp(0.3f, pulseInterval, distance / pulseTriggerDistance);

            if (pulseTimer >= currentInterval)
            {
                pulseTimer = 0f;
                
                // ==========================================
                // ★修正2：波紋の発生地点を「レーダー内の正しい相対位置」に計算
                // ==========================================
                float distanceRatio = Mathf.Clamp01(distance / sonarRange);
                float uiX = Mathf.Sin(angleRad) * distanceRatio * radarUIRadius;
                float uiY = Mathf.Cos(angleRad) * distanceRatio * radarUIRadius;

                if (pulseRing != null)
                {
                    pulseRing.localPosition = new Vector3(uiX, uiY, 0); 
                    pulseRing.sizeDelta = Vector2.zero; // サイズを0にリセット
                    currentPulseAlpha = 1f; // 不透明度をMAX（100%）にする
                }

                if (sonarAudioSource != null && pulseSound != null)
                {
                    sonarAudioSource.PlayOneShot(pulseSound, 0.7f);
                }
            }
        }

        // ==========================================
        // 波紋のアニメーション（広がりとフェードアウト）
        // ==========================================
        if (pulseRing != null && currentPulseAlpha > 0f)
        {
            pulseRing.sizeDelta += new Vector2(pulseExpandSpeed, pulseExpandSpeed) * Time.deltaTime;
            currentPulseAlpha -= pulseFadeSpeed * Time.deltaTime;

            UnityEngine.UI.Image ringImage = pulseRing.GetComponent<UnityEngine.UI.Image>();
            if (ringImage != null)
            {
                Color c = ringImage.color;
                c.a = Mathf.Max(0f, currentPulseAlpha);
                ringImage.color = c;
            }
        }
    }

    // ==========================================
    // 補助メソッド群（既存コードの整理）
    // ==========================================
    private void DrawEchoes(float angleDelta)
    {
        int raysToShoot = Mathf.Max(1, Mathf.CeilToInt(angleDelta / scanResolution));
        for (int r = 0; r < raysToShoot; r++)
        {
            float fractionalAngle = currentSweepAngle - (angleDelta * ((float)r / raysToShoot));
            if (fractionalAngle <= -360f) fractionalAngle += 360f;

            float horizontalRad = -fractionalAngle * Mathf.Deg2Rad;
            Vector3 direction = player.rotation * new Vector3(Mathf.Sin(horizontalRad), 0, Mathf.Cos(horizontalRad));
            Vector3 rayOrigin = player.position + new Vector3(0, 1.0f, 0);

            if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, sonarRange, wallLayer))
            {
                float r0 = (hit.distance / sonarRange) * centerPixel;
                float maxR = centerPixel;

                for (float radius = r0; radius < maxR; radius += 0.5f)
                {
                    float t = (radius - r0) / (maxR - r0);
                    Color32 drawColor = shadowGradient.Evaluate(t);

                    int px = Mathf.RoundToInt(centerPixel + Mathf.Sin(horizontalRad) * radius);
                    int py = Mathf.RoundToInt(centerPixel + Mathf.Cos(horizontalRad) * radius);

                    if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                    {
                        pixelBuffer[py * textureSize + px] = drawColor;
                    }
                }
            }
        }
        sonarTexture.SetPixels32(pixelBuffer);
        sonarTexture.Apply();
    }

    private void FadeOutPixels()
    {
        for (int i = 0; i < pixelBuffer.Length; i++)
        {
            if (pixelBuffer[i].a > 0)
            {
                pixelBuffer[i].a = (byte)Mathf.Max(0, pixelBuffer[i].a - fadeSpeed);
            }
        }
    }

    private void UpdateBlips()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null || !targets[i].enabled || !targets[i].gameObject.activeInHierarchy)
            {
                if (blips[i] != null && blips[i].activeSelf) blips[i].SetActive(false);
                continue; 
            }

            Vector3 relativePos = targets[i].transform.position - player.position;
            float distance = new Vector2(relativePos.x, relativePos.z).magnitude;

            if (distance <= sonarRange)
            {
                float angle = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
                angle -= player.eulerAngles.y;

                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(-currentSweepAngle, angle));
                UnityEngine.UI.Image blipImage = blips[i].GetComponent<UnityEngine.UI.Image>();

                if (angleDiff < 5f) 
                {
                    blips[i].SetActive(true);
                    if (blipImage != null)
                    {
                        Color c = blipImage.color;
                        c.a = 1f; 
                        blipImage.color = c;
                    }
                    float distanceRatio = distance / sonarRange;
                    float angleRad = angle * Mathf.Deg2Rad;
                    float uiX = Mathf.Sin(angleRad) * distanceRatio * radarUIRadius;
                    float uiY = Mathf.Cos(angleRad) * distanceRatio * radarUIRadius;
                    blips[i].transform.localPosition = new Vector3(uiX, uiY, 0);
                }

                if (blips[i].activeSelf && blipImage != null)
                {
                    Color c = blipImage.color;
                    c.a -= Time.deltaTime * 0.5f; 
                    blipImage.color = c;
                    if (c.a <= 0f) blips[i].SetActive(false);
                }
            }
            else blips[i].SetActive(false); 
        }
    }

    private void UpdateStatusDisplay()
    {
        if (subStatus != null && statusDisplayText != null)
        {
            float heading = Mathf.Repeat(player.eulerAngles.y, 360f);
            string currentGearName = "UNKNOWN";
            SubmarineController subController = player.GetComponent<SubmarineController>();
            if (subController != null)
            {
                currentGearName = subController.gears[subController.currentGearIndex].gearName;
            }

            statusDisplayText.text = 
                $"HULL INTEG : {subStatus.currentHP:F0} / {subStatus.maxHP:F0}\n\n" +
                $"GEAR       : {currentGearName}\n" +
                $"SPEED      : {subStatus.currentSpeed:F1} KTS\n" +
                $"TURN RATE  : {subStatus.currentTurnRate:F1} DEG/S\n" +
                $"HEADING    : {heading:F0}°";
        }
    }
}