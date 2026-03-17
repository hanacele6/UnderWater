using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SonarManager : MonoBehaviour
{
    public static SonarManager Instance;

    [Header("UI References")]
    public Transform radarCenter;
    public RawImage sonarDisplayImage; 

    public RectTransform compassRing;
    public Transform[] compassLabels;
    public RectTransform submarineOutlineMarker;

    [Header("Information Display")]
    public SubmarineStatus subStatus;        
    public TMPro.TMP_Text statusDisplayText;
    public GameObject fixedBlipPrefab;

    [Header("Sonar Settings")]
    public float pulseScanSpeed = 30f; 
    public Transform player;
    public float sonarRange = 50f;
    private float radarUIRadius;
    public LayerMask wallLayer; 
    public LayerMask targetLayer;

    [Header("Echo Visuals (Texture)")]
    public int textureSize = 512;
    public float scanResolution = 0.5f; 
    public byte fadeSpeed = 3;

    [Tooltip("光のにじみの広がり具合（0にするとくっきり）")]
    public float echoBloomSpread = 2.5f;
    [Tooltip("にじみの強さ（透明度）")]
    public float echoBloomIntensity = 0.3f;
    [Tooltip("壁の中の点を判定する間隔（小さいほど点が増える）")]
    public float innerWallStepSize = 0.5f;

    [Header("Barotrauma Sonar Settings")]
    public bool isRealtimeTracking = true;
    public bool penetrateWalls = true; 
    [Range(0f, 1f)] public float innerWallDensity = 0.4f;
    public float innerWallNoiseScale = 0.1f;
    public int baseBlobRadius = 4;
    public float textureUpdateInterval = 0.05f;
    private float textureUpdateTimer = 0f;

    [Header("Wall Visuals (壁・地形の見た目)")]
    [Tooltip("壁用のグラデーション。時間が経つと色が緩やかに揺らぎます")]
    public Gradient wallGradient;
    [Tooltip("壁の中の光の大きさ")]
    public float innerWallEchoSizeMin = 0.4f;
    public float innerWallEchoSizeMax = 0.8f;
    [Tooltip("壁の中の光の明るさ")]
    public float innerWallIntensityMin = 0.1f;
    public float innerWallIntensityMax = 0.3f;
    [Tooltip("壁の色の揺らぐスピード")]
    public float wallColorJitterSpeed = 0.5f;

    [Header("Biological Target Visuals (生物の見た目)")]
    [Tooltip("生物用のグラデーション。色が不気味に激しく揺らぎます")]
    public Gradient bioGradient;
    [Tooltip("生物の光の大きさ")]
    public float bioEchoSizeMin = 1.5f;
    public float bioEchoSizeMax = 2.5f;
    [Tooltip("色の揺らぐスピード")]
    public float bioColorJitterSpeedMin = 5f;
    public float bioColorJitterSpeedMax = 15f;

    [Header("Mission Navigation")]
    public RectTransform missionMarker;
    public float pulseExpandSpeed = 150f;
    public float pulseFadeSpeed = 1.0f;
    public float pulseTriggerDistance = 30f;
    
    [Header("Audio")]
    public AudioSource sonarAudioSource;
    public AudioClip pulseSound;
    public AudioClip pingSound;

    [Header("Ring Colors")]
    public Color mainPulseColor = Color.green;
    public Color noiseRangeColor = new Color(1f, 0f, 0f, 0.3f); 
    public Color missionPulseColor = Color.yellow;

    private UIRing mainPulseRing; 
    private UIRing noiseRangeCircle; 
    private UIRing missionPulseRing; 
    
    private Transform currentMissionTarget;
    private float missionPulseTimer = 0f;
    private float missionPulseInterval = 1.5f; 
    private float missionPulseAlpha = 0f; 

    private List<SonarTarget> targets = new List<SonarTarget>();
    private List<GameObject> fixedBlips = new List<GameObject>();
    
    private float currentPulseDistance = 0f; 
    private float prevPulseDistance = 0f;
    private int rayCount;

    private Texture2D sonarTexture;
    private Color32[] pixelBuffer; 
    private int centerPixel;

    private class EchoPoint
    {
        public Vector3 worldPos;
        public float distance;
        public float intensity;
        public float staticPx;
        public float staticPy;
        public float proceduralSize = 1f;
        public Gradient targetGradient; // ★使用するグラデーションへの参照
        public float colorBaseT = 1f;
        public float colorJitterSpeed = 0f;
        public bool isBiological = false;
        public bool isInteriorNoise = false;
    }

    // In/Outを判別するための構造体
    struct WallHit : System.IComparable<WallHit> 
    {
        public float distance;
        public bool isEntry; // trueなら表面(In)、falseなら裏面(Out)
        public int CompareTo(WallHit other) => distance.CompareTo(other.distance);
    }

    [System.Serializable]
    public struct MissionSonarData {
        public Transform target;
        public string name;
    }

    private class ActiveMissionUI
    {
        public MissionSonarData data;
        public RectTransform marker;
        public UIRing pulseRing;
        public float pulseTimer;
        public float pulseAlpha;
        public TMPro.TMP_Text text; 
    }
    private List<ActiveMissionUI> activeMissions = new List<ActiveMissionUI>();

    [Header("Structure Navigation")]
    [Tooltip("ストラクチャー用のUIプレハブ（ImageとTMP_Textを含むもの）")]
    public RectTransform structureMarkerPrefab;
    public RectTransform missionMarkerPrefab;
    public float structureScanRadius = 150f;
    public float structureScanInterval = 1.0f;
    private float structureScanTimer = 0f;

    private class ActiveStructureUI
    {
        public StructureMarker data;
        public RectTransform marker;
        public TMPro.TMP_Text text;
    }
    private List<ActiveStructureUI> activeStructures = new List<ActiveStructureUI>();

    private List<EchoPoint> pendingEchoes = new List<EchoPoint>();
    private List<EchoPoint> activeEchoes = new List<EchoPoint>();

    void Awake() { Instance = this; }

    void Start()
    {
        RectTransform rt = sonarDisplayImage.GetComponent<RectTransform>();
        radarUIRadius = rt.rect.width / 2f;

        rayCount = Mathf.Max(1, Mathf.CeilToInt(360f / scanResolution));

        mainPulseRing = CreateAutoRing("Auto_MainPulseRing", mainPulseColor, 2f, false);
        noiseRangeCircle = CreateAutoRing("Auto_NoiseRangeCircle", noiseRangeColor, 0f, true); 
        //missionPulseRing = CreateAutoRing("Auto_MissionPulseRing", missionPulseColor, 2f, false);
        if (missionMarker != null) missionMarker.gameObject.SetActive(false);

        SonarTarget[] foundTargets = FindObjectsOfType<SonarTarget>();
        foreach (SonarTarget target in foundTargets)
        {
            targets.Add(target);
            if (target.targetShape == SonarTargetShape.UI_Prefab)
            {
                GameObject newBlip = Instantiate(fixedBlipPrefab, radarCenter);
                newBlip.SetActive(false);
                SetupBlipUI(newBlip, target);
                fixedBlips.Add(newBlip);
            }
            
            else
            {
                fixedBlips.Add(null); 
            }
        }

        sonarTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        pixelBuffer = new Color32[textureSize * textureSize];
        System.Array.Clear(pixelBuffer, 0, pixelBuffer.Length);
        sonarTexture.SetPixels32(pixelBuffer);
        sonarTexture.Apply();
        sonarDisplayImage.texture = sonarTexture;
        centerPixel = textureSize / 2;

        if (missionMarker != null) missionMarker.gameObject.SetActive(false);
        if (submarineOutlineMarker != null) submarineOutlineMarker.gameObject.SetActive(true);
        missionPulseAlpha = 0f;

        TriggerSonarPing(); 
    }

    private UIRing CreateAutoRing(string objName, Color color, float thickness, bool fill)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(radarCenter, false);
        
        UIRing ring = go.AddComponent<UIRing>();
        ring.color = color;
        ring.thickness = thickness;
        ring.fill = fill;
        ring.raycastTarget = false; 
        
        ring.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        ring.rectTransform.anchoredPosition = Vector2.zero;
        ring.rectTransform.sizeDelta = Vector2.zero;

        return ring;
    }

    private void SetupBlipUI(GameObject blipObj, SonarTarget target)
    {
        Image blipImage = blipObj.GetComponent<Image>();
        TMPro.TextMeshProUGUI blipText = blipObj.GetComponentInChildren<TMPro.TextMeshProUGUI>(); 

        if (blipImage != null)
        {
            if (blipText != null) blipText.gameObject.SetActive(false);
            switch (target.targetType)
            {
                case SubmarineTargetType.Mine: blipImage.color = Color.red; break;
                case SubmarineTargetType.HostileBio: blipImage.color = new Color(1f, 0.4f, 0f); break;
                case SubmarineTargetType.NeutralBio: blipImage.color = Color.cyan; break;
                case SubmarineTargetType.Item: blipImage.color = Color.yellow; break;
                
            }
        }
    }

    public void SetMissionTargets(List<MissionSonarData> targets)
    {
        for (int i = activeMissions.Count - 1; i >= 0; i--)
        {
            if (!targets.Exists(t => t.target == activeMissions[i].data.target))
            {
                if (activeMissions[i].marker != null) Destroy(activeMissions[i].marker.gameObject);
                if (activeMissions[i].pulseRing != null) Destroy(activeMissions[i].pulseRing.gameObject);
                activeMissions.RemoveAt(i);
            }
        }

        foreach (var t in targets)
        {
            if (!activeMissions.Exists(m => m.data.target == t.target))
            {
                GameObject newMarkerObj = Instantiate(missionMarkerPrefab.gameObject, radarCenter);
                newMarkerObj.SetActive(true);

                UIRing newRing = CreateAutoRing("Auto_MissionPulseRing", missionPulseColor, 2f, false);

                activeMissions.Add(new ActiveMissionUI {
                    data = t,
                    marker = newMarkerObj.GetComponent<RectTransform>(),
                    pulseRing = newRing,
                    pulseTimer = 0f,
                    pulseAlpha = 0f,
                    text = newMarkerObj.GetComponentInChildren<TMPro.TMP_Text>() // ★テキストを取得
                });
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateNoiseRing();

        structureScanTimer -= Time.deltaTime;
        if (structureScanTimer <= 0f) { ScanNearbyStructures(); structureScanTimer = structureScanInterval; }
        UpdateStructureMarkers();

        prevPulseDistance = currentPulseDistance;
        currentPulseDistance += pulseScanSpeed * Time.deltaTime;

        for (int i = pendingEchoes.Count - 1; i >= 0; i--)
        {
            if (pendingEchoes[i].distance <= currentPulseDistance)
            {
                EchoPoint echo = pendingEchoes[i];
                if (!isRealtimeTracking)
                {
                    Vector3 relativePos = echo.worldPos - player.position;
                    float angle = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
                    angle -= player.eulerAngles.y; 
                    
                    float uiR = (echo.distance / sonarRange) * centerPixel;
                    float rad = angle * Mathf.Deg2Rad;
                    echo.staticPx = centerPixel + Mathf.Sin(rad) * uiR;
                    echo.staticPy = centerPixel + Mathf.Cos(rad) * uiR;
                }
                activeEchoes.Add(echo);
                pendingEchoes.RemoveAt(i);
            }
        }

        textureUpdateTimer += Time.deltaTime;
        if (textureUpdateTimer >= textureUpdateInterval)
        {
            DrawBarotraumaEchoes();
            textureUpdateTimer = 0f;
        }

        if (mainPulseRing != null)
        {
            float pulseUIRadius = (currentPulseDistance / sonarRange) * radarUIRadius;
            mainPulseRing.rectTransform.sizeDelta = new Vector2(pulseUIRadius * 2f, pulseUIRadius * 2f);

            Color c = mainPulseRing.color;
            c.a = Mathf.Clamp01(1.0f - (currentPulseDistance / sonarRange));
            mainPulseRing.color = c;
        }

        UpdateBlips();

        if (currentPulseDistance >= sonarRange) TriggerSonarPing();

        if (compassRing != null)
        {
            compassRing.localEulerAngles = new Vector3(0, 0, player.eulerAngles.y);
            if (compassLabels != null) foreach (Transform label in compassLabels) label.eulerAngles = Vector3.zero;
        }
        UpdateStatusDisplay();
        UpdateMissionNavigation();
    }

    

    private void TriggerSonarPing()
    {
        currentPulseDistance = 0f;
        prevPulseDistance = 0f;
        
        if (mainPulseRing != null) mainPulseRing.rectTransform.sizeDelta = Vector2.zero;
        if (sonarAudioSource != null && pingSound != null) sonarAudioSource.PlayOneShot(pingSound, 1.0f);

        pendingEchoes.Clear();

        for (int i = 0; i < rayCount; i++)
        {
            float angleDeg = i * scanResolution;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 direction = Quaternion.Euler(0, angleDeg + player.eulerAngles.y, 0) * Vector3.forward;
            Vector3 rayOrigin = player.position + new Vector3(0, 1.0f, 0);

            if (penetrateWalls)
            {                
                List<WallHit> hitList = new List<WallHit>();

                // 1. 前方へのレイキャスト（表面＝Entryを取得）
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, direction, sonarRange, wallLayer);
                foreach (var hit in hits) hitList.Add(new WallHit { distance = hit.distance, isEntry = true });

                // 2. 後方からの逆レイキャスト（裏面＝Exitを取得）
                Vector3 reverseOrigin = rayOrigin + (direction * sonarRange);
                Vector3 reverseDirection = -direction;
                RaycastHit[] reverseHits = Physics.RaycastAll(reverseOrigin, reverseDirection, sonarRange, wallLayer);
                foreach (var hit in reverseHits)
                {
                    float distFromPlayer = sonarRange - hit.distance;
                    if (distFromPlayer > 0 && distFromPlayer <= sonarRange)
                    {
                        hitList.Add(new WallHit { distance = distFromPlayer, isEntry = false });
                    }
                }

                // 距離順に並び替え
                hitList.Sort();

                List<float> outlineBoundaries = new List<float>();
                List<Vector2> solidRanges = new List<Vector2>();

                int insideCount = 0;
                float currentStart = -1f;

                // 3. カウント方式で重なりをマージする
                foreach (var h in hitList)
                {
                    if (h.isEntry)
                    {
                        if (insideCount == 0)
                        {
                            currentStart = h.distance;
                            outlineBoundaries.Add(h.distance); // 完全に外側にある「表面」だけ輪郭にする
                        }
                        insideCount++;
                    }
                    else
                    {
                        insideCount = Mathf.Max(0, insideCount - 1); // 0未満にならないようフェールセーフ
                        if (insideCount == 0 && currentStart != -1f)
                        {
                            outlineBoundaries.Add(h.distance); // 完全に外に出た「裏面」だけ輪郭にする
                            solidRanges.Add(new Vector2(currentStart, h.distance));
                            currentStart = -1f;
                        }
                    }
                }
                if (insideCount > 0 && currentStart != -1f)
                {
                    solidRanges.Add(new Vector2(currentStart, sonarRange));
                }

                // --- 描画キューへの登録 ---

                // 外枠（くっきりした線）の登録
                // 光りすぎるのを防ぐため、intensityを 1.0f から少し下げます（例: 0.7f）
                foreach (float d in outlineBoundaries)
                {
                    Vector3 pos = rayOrigin + direction * d;
                    pendingEchoes.Add(new EchoPoint { 
                        worldPos = pos, 
                        distance = d, 
                        intensity = 0.7f, 
                        targetGradient = wallGradient,
                        colorBaseT = 1.0f, 
                        colorJitterSpeed = wallColorJitterSpeed
                    });
                }

                // 内部のモヤモヤ（ノイズ）の登録
                foreach (Vector2 range in solidRanges)
                {
                    float startDist = range.x;
                    float endDist = range.y;

                    float maxDepth = 30.0f; 
                    float actualEndDist = Mathf.Min(endDist - 0.1f, startDist + maxDepth);

                    // 進行方向に対して真横(90度)のベクトル
                    Vector3 perpDir = new Vector3(-direction.z, 0, direction.x);

                    float safeStepSize = Mathf.Max(1.5f, innerWallStepSize);

                    for (float d = startDist + 0.1f; d < actualEndDist; d += safeStepSize)
                    {
                        if (Random.value > 0.5f) continue;

                        float lateralOffset = Random.Range(-safeStepSize, safeStepSize);
                        Vector3 pos = rayOrigin + direction * d + (perpDir * lateralOffset);
                        
                        float noise = Mathf.PerlinNoise(pos.x * innerWallNoiseScale, pos.z * innerWallNoiseScale);

                        if (noise < innerWallDensity)
                        {
                            pendingEchoes.Add(new EchoPoint {
                                worldPos = pos,
                                distance = d,
                                intensity = Random.Range(innerWallIntensityMin, innerWallIntensityMax), 
                                proceduralSize = Random.Range(innerWallEchoSizeMin, innerWallEchoSizeMax) ,
                                targetGradient = wallGradient,
                                colorBaseT = 1.0f,
                                colorJitterSpeed = wallColorJitterSpeed,
                                isBiological = false,
                                isInteriorNoise = true 
                            });
                        }
                    }
                }
            }
            else
            {
                if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, sonarRange, wallLayer))
                {
                    pendingEchoes.Add(new EchoPoint { worldPos = hit.point, distance = hit.distance, intensity = 1.0f, targetGradient = wallGradient });
                }
            }
        }
    }

    private void DrawBarotraumaEchoes()
    {
        System.Array.Clear(pixelBuffer, 0, pixelBuffer.Length);

        for (int i = activeEchoes.Count - 1; i >= 0; i--)
        {
            EchoPoint echo = activeEchoes[i];
            
            echo.intensity -= (fadeSpeed / 255f) * textureUpdateInterval * 60f;
            if (echo.intensity <= 0)
            {
                activeEchoes.RemoveAt(i);
                continue;
            }

            float px, py;
            if (isRealtimeTracking)
            {
                Vector3 relativePos = echo.worldPos - player.position;
                float angle = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg - player.eulerAngles.y;
                float dist = new Vector2(relativePos.x, relativePos.z).magnitude;
                
                if (dist > sonarRange) continue; 

                float uiR = (dist / sonarRange) * centerPixel;
                float rad = angle * Mathf.Deg2Rad;
                px = centerPixel + Mathf.Sin(rad) * uiR;
                py = centerPixel + Mathf.Cos(rad) * uiR;
            }
            else
            {
                px = echo.staticPx;
                py = echo.staticPy;
            }

            Color baseColor;
            float t;
            if (echo.isBiological)
            {
                // 生物の場合は、基準色を中心に激しくブルブル揺れる
                t = echo.colorBaseT + Mathf.Sin(Time.time * echo.colorJitterSpeed) * 0.1f;
            }
            else
            {
                // 壁の場合は、PingPongを使ってグラデーション全体(0〜1)をゆっくり行ったり来たりする！
                t = Mathf.PingPong(Time.time * echo.colorJitterSpeed, 1f);
            }
            if (echo.targetGradient != null)
            {
                baseColor = echo.targetGradient.Evaluate(Mathf.Clamp01(t));
            }
            else 
            {
                // 万が一グラデーションが割り当たっていない場合の安全策
                baseColor = Color.white; 
            }

            DrawBlobAdditive(px, py, echo.intensity, echo.proceduralSize, baseColor, echo.isBiological, echo.isInteriorNoise);
        }

        sonarTexture.SetPixels32(pixelBuffer);
        sonarTexture.Apply();
    }

    private void DrawBlobAdditive(float cx, float cy, float intensity, float sizeMultiplier, Color baseColor, bool isBiological, bool isInteriorNoise = false)
    {
        int centerX = Mathf.RoundToInt(cx);
        int centerY = Mathf.RoundToInt(cy);

        if (isInteriorNoise)
        {
            DrawSingleBlobPass(centerX, centerY, baseColor, echoBloomIntensity, baseBlobRadius * 1.5f * sizeMultiplier, intensity); 
            return; 
        }

        DrawSingleBlobPass(centerX, centerY, baseColor, 1.0f, baseBlobRadius * 0.5f * sizeMultiplier, intensity); 
        
        if (isBiological)
        {
            DrawSingleBlobPass(centerX, centerY, baseColor, 0.6f, baseBlobRadius * 2.0f * sizeMultiplier, intensity); 
            DrawSingleBlobPass(centerX, centerY, baseColor, 0.3f, baseBlobRadius * 3.5f * sizeMultiplier, intensity); 
        }
        else
        {
            DrawSingleBlobPass(centerX, centerY, baseColor, echoBloomIntensity, baseBlobRadius * 1.5f * sizeMultiplier, intensity); 
            if (echoBloomSpread > 0f && echoBloomIntensity > 0f)
            {
                DrawSingleBlobPass(centerX, centerY, baseColor, echoBloomIntensity * 0.5f, baseBlobRadius * echoBloomSpread * sizeMultiplier, intensity); 
            }
        }
    }

    private void DrawSingleBlobPass(int cx, int cy, Color col, float maxAlpha, float radius, float intensity)
    {
        int r = Mathf.RoundToInt(radius);
        byte baseR = (byte)(col.r * 255);
        byte baseG = (byte)(col.g * 255);
        byte baseB = (byte)(col.b * 255);

        for (int y = -r; y <= r; y++)
        {
            for (int x = -r; x <= r; x++)
            {
                float dist = Mathf.Sqrt(x * x + y * y);
                if (dist <= radius)
                {
                    int px = cx + x;
                    int py = cy + y;
                    
                    if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                    {
                        float alphaRaw = (1f - (dist / radius)) * maxAlpha;
                        
                        float globalBrightnessMultiplier = 0.6f; 
                        
                        byte alphaToAdd = (byte)(alphaRaw * intensity * globalBrightnessMultiplier * 255);

                        int index = py * textureSize + px;
                        Color32 current = pixelBuffer[index];

                        // 単純加算だとすぐ白飛びするので、最大値を少し抑えたり、加算カーブを緩やかにするのも手です
                        byte newAlpha = (byte)Mathf.Min(255, current.a + alphaToAdd);
                        pixelBuffer[index] = new Color32(baseR, baseG, baseB, newAlpha);
                    }
                }
            }
        }
    }

    private void UpdateNoiseRing()
    {
        if (noiseRangeCircle == null) return;

        SubmarineController subController = player.GetComponent<SubmarineController>();
        if (subController != null)
        {
            float currentNoise = subController.GetCurrentNoiseRadius();
            float noiseUIRadius = (currentNoise / sonarRange) * radarUIRadius;
            Vector2 targetSize = new Vector2(noiseUIRadius * 2f, noiseUIRadius * 2f);
            noiseRangeCircle.rectTransform.sizeDelta = Vector2.Lerp(noiseRangeCircle.rectTransform.sizeDelta, targetSize, Time.deltaTime * 5f);
        }
    }

    private void UpdateBlips()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null || !targets[i].enabled || !targets[i].gameObject.activeInHierarchy)
            {
                if (fixedBlips[i] != null && fixedBlips[i].activeSelf) fixedBlips[i].SetActive(false);
                continue; 
            }

            Vector3 relativePos = targets[i].transform.position - player.position;
            float distance = new Vector2(relativePos.x, relativePos.z).magnitude;

            // ==========================================
            // 通常のパルスで光るターゲット（生物・アイテム等）の処理
            // ==========================================
            // これらはソナー範囲内 (distance <= sonarRange) にいる時だけ処理する
            if (distance <= sonarRange)
            {
                bool isHitByPulse = (distance > prevPulseDistance && distance <= currentPulseDistance);

                // 【不定形ターゲット（生物）】
                if (targets[i].targetShape == SonarTargetShape.Procedural_Texture)
                {
                    if (isHitByPulse)
                    {
                        float angle = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg - player.eulerAngles.y;
                        float uiR = (distance / sonarRange) * centerPixel;
                        float rad = angle * Mathf.Deg2Rad;
                        
                        activeEchoes.Add(new EchoPoint {
                            worldPos = targets[i].transform.position,
                            distance = distance,
                            intensity = 1.0f,
                            proceduralSize = Random.Range(bioEchoSizeMin, bioEchoSizeMax),
                            staticPx = centerPixel + Mathf.Sin(rad) * uiR,
                            staticPy = centerPixel + Mathf.Cos(rad) * uiR,
                            isBiological = true,
                            targetGradient = bioGradient,
                            colorBaseT = Random.Range(0.1f, 0.9f),
                            colorJitterSpeed = Random.Range(bioColorJitterSpeedMin, bioColorJitterSpeedMax)
                        });
                    }
                    continue; 
                }

                // 【固定UIターゲット（アイテム・機雷等）】
                UnityEngine.UI.Image blipImage = fixedBlips[i].GetComponent<UnityEngine.UI.Image>();

                if (isHitByPulse) 
                {
                    fixedBlips[i].SetActive(true);
                    if (blipImage != null)
                    {
                        Color c = blipImage.color;
                        c.a = 1f; 
                        blipImage.color = c;
                    }
                }

                if (fixedBlips[i].activeSelf)
                {
                    if (blipImage != null)
                    {
                        Color c = blipImage.color;
                        c.a -= Time.deltaTime * 0.5f; 
                        blipImage.color = c;
                        if (c.a <= 0f) fixedBlips[i].SetActive(false);
                    }

                    if (isRealtimeTracking || isHitByPulse)
                    {
                        float angle = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
                        angle -= player.eulerAngles.y;
                        float distanceRatio = distance / sonarRange;
                        float angleRad = angle * Mathf.Deg2Rad;
                        float uiX = Mathf.Sin(angleRad) * distanceRatio * radarUIRadius;
                        float uiY = Mathf.Cos(angleRad) * distanceRatio * radarUIRadius;
                        fixedBlips[i].transform.localPosition = new Vector3(uiX, uiY, 0);
                    }
                }
            }
            else
            {
                // ソナー範囲外なら非表示にする（※AlwaysVisible_Marker以外）
                if (fixedBlips[i] != null) fixedBlips[i].SetActive(false); 
            }
        }
    }


    private void UpdateMissionNavigation()
    {
        foreach (var mission in activeMissions)
        {
            if (mission.data.target == null) continue;

            Vector3 dir = mission.data.target.position - player.position;
            float distance = new Vector2(dir.x, dir.z).magnitude;

            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float relativeAngle = targetAngle - player.eulerAngles.y;
            float angleRad = relativeAngle * Mathf.Deg2Rad;

            // ==========================================
            // ★UI位置の計算（距離をClamp01で制限し、常に円周内に留める）
            // ==========================================
            float distanceRatio = Mathf.Clamp01(distance / sonarRange);
            float uiX = Mathf.Sin(angleRad) * distanceRatio * radarUIRadius;
            float uiY = Mathf.Cos(angleRad) * distanceRatio * radarUIRadius;
            
            mission.marker.localPosition = new Vector3(uiX, uiY, 0);
            mission.marker.localEulerAngles = Vector3.zero; // 回転させず常に正立させる

            if (mission.text != null)
            {
                // ソナー外の時は少し暗い色にするなどの演出
                string colorTag = distance > sonarRange ? "<color=#AAAAAA>" : "<color=#FFD700>";
                mission.text.text = $"{colorTag}{mission.data.name}\n<size=70%>{distance:F0}m</size></color>";
            }

            // ==========================================
            // パルスリングの処理
            // ==========================================
            if (distance <= pulseTriggerDistance)
            {
                mission.pulseTimer += Time.deltaTime;
                float currentInterval = Mathf.Lerp(0.3f, missionPulseInterval, distance / pulseTriggerDistance);

                if (mission.pulseTimer >= currentInterval)
                {
                    mission.pulseTimer = 0f;

                    if (mission.pulseRing != null)
                    {
                        mission.pulseRing.rectTransform.localPosition = new Vector3(uiX, uiY, 0); 
                        mission.pulseRing.rectTransform.sizeDelta = Vector2.zero; 
                        mission.pulseAlpha = 1f; 
                    }

                    if (sonarAudioSource != null && pulseSound != null)
                    {
                        sonarAudioSource.PlayOneShot(pulseSound, 0.7f);
                    }
                }
            }

            if (mission.pulseRing != null && mission.pulseAlpha > 0f)
            {
                mission.pulseRing.rectTransform.sizeDelta += new Vector2(pulseExpandSpeed, pulseExpandSpeed) * Time.deltaTime;
                mission.pulseAlpha -= pulseFadeSpeed * Time.deltaTime;

                Color c = mission.pulseRing.color;
                c.a = Mathf.Max(0f, mission.pulseAlpha);
                mission.pulseRing.color = c;
            }
        }
    }

    
    private void UpdateStatusDisplay()
    {
        if (subStatus != null && statusDisplayText != null)
        {
            float heading = Mathf.Repeat(player.eulerAngles.y, 360f);
            string currentGearName = "UNKNOWN";
            SubmarineController subController = player.GetComponent<SubmarineController>();
            if (subController != null) currentGearName = subController.gears[subController.currentGearIndex].gearName;

            statusDisplayText.text = 
                $"HULL INTEG : {subStatus.currentHP:F0} / {subStatus.maxHP:F0}\n\n" +
                $"GEAR       : {currentGearName}\n" +
                $"SPEED      : {subStatus.currentSpeed:F1} KTS\n" +
                $"TURN RATE  : {subStatus.currentTurnRate:F1} DEG/S\n" +
                $"HEADING    : {heading:F0}°";
        }
    }

    private void ScanNearbyStructures()
    {
        if (structureMarkerPrefab == null) return;

        StructureMarker[] markers = FindObjectsOfType<StructureMarker>();
        List<StructureMarker> nearbyMarkers = new List<StructureMarker>();

        // ソナー範囲内のストラクチャーをリストアップ
        foreach (var m in markers)
        {
            if (Vector3.Distance(player.position, m.transform.position) <= structureScanRadius) 
                nearbyMarkers.Add(m);
        }

        // 範囲外になった古いUIを削除
        for (int i = activeStructures.Count - 1; i >= 0; i--)
        {
            if (!nearbyMarkers.Contains(activeStructures[i].data))
            {
                if (activeStructures[i].marker != null) Destroy(activeStructures[i].marker.gameObject);
                activeStructures.RemoveAt(i);
            }
        }

        // 新しいUIを追加
        foreach (var m in nearbyMarkers)
        {
            if (!activeStructures.Exists(s => s.data == m))
            {
                GameObject newObj = Instantiate(structureMarkerPrefab.gameObject, radarCenter);
                newObj.SetActive(true);
                activeStructures.Add(new ActiveStructureUI {
                    data = m,
                    marker = newObj.GetComponent<RectTransform>(),
                    text = newObj.GetComponentInChildren<TMPro.TMP_Text>()
                });
            }
        }
    }

    private void UpdateStructureMarkers()
    {
        foreach (var s in activeStructures)
        {
            if (s.data == null) continue;

            Vector3 dir = s.data.transform.position - player.position;
            float distance = new Vector2(dir.x, dir.z).magnitude;

            s.marker.gameObject.SetActive(true);

            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float relativeAngle = targetAngle - player.eulerAngles.y;
            float angleRad = relativeAngle * Mathf.Deg2Rad;

            float distanceRatio = Mathf.Clamp01(distance / sonarRange);
            
            float uiX = Mathf.Sin(angleRad) * distanceRatio * radarUIRadius;
            float uiY = Mathf.Cos(angleRad) * distanceRatio * radarUIRadius;

            s.marker.localPosition = new Vector3(uiX, uiY, 0);
            s.marker.localEulerAngles = Vector3.zero; 

            if (s.text != null)
            {
                // （おまけ）ソナー外にいる時は、文字を少し暗くして区別することも可能です
                string colorTag = distance > sonarRange ? "<color=#888888>" : "<color=#FFFFFF>";
                s.text.text = $"{colorTag}{s.data.structureName}\n<size=70%>{distance:F0}m</size></color>";
            }
        }
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public class UIRing : MaskableGraphic
{
    public float thickness = 2f;
    public int segments = 64;
    public bool fill = false;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float outerRadius = rectTransform.rect.width / 2f;
        float innerRadius = fill ? 0f : Mathf.Max(0, outerRadius - thickness);
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = Mathf.Deg2Rad * (i * angleStep);
            float angle2 = Mathf.Deg2Rad * ((i + 1) * angleStep);

            Vector2 outer1 = new Vector2(Mathf.Sin(angle1), Mathf.Cos(angle1)) * outerRadius;
            Vector2 outer2 = new Vector2(Mathf.Sin(angle2), Mathf.Cos(angle2)) * outerRadius;
            Vector2 inner1 = new Vector2(Mathf.Sin(angle1), Mathf.Cos(angle1)) * innerRadius;
            Vector2 inner2 = new Vector2(Mathf.Sin(angle2), Mathf.Cos(angle2)) * innerRadius;

            UIVertex v1 = UIVertex.simpleVert; v1.color = color; v1.position = inner1;
            UIVertex v2 = UIVertex.simpleVert; v2.color = color; v2.position = outer1;
            UIVertex v3 = UIVertex.simpleVert; v3.color = color; v3.position = outer2;
            UIVertex v4 = UIVertex.simpleVert; v4.color = color; v4.position = inner2;

            int startIndex = vh.currentVertCount;
            vh.AddVert(v1); vh.AddVert(v2); vh.AddVert(v3); vh.AddVert(v4);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }
}