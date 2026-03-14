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

    [Header("Information Display")]
    public SubmarineStatus subStatus;        
    public TMPro.TMP_Text statusDisplayText;
    public GameObject blipPrefab;

    [Header("Sonar Settings")]
    public float pulseScanSpeed = 30f; 
    public Transform player;
    public float sonarRange = 50f;
    private float radarUIRadius;
    public LayerMask wallLayer; 

    [Header("Echo Visuals (Texture)")]
    public int textureSize = 512;
    // ★復活：元のスキャン解像度（0.5で細かく滑らかに描画）
    public float scanResolution = 0.5f; 
    public Gradient shadowGradient;
    public byte fadeSpeed = 3;

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
    private List<GameObject> blips = new List<GameObject>();
    
    private float currentPulseDistance = 0f; 
    private float prevPulseDistance = 0f;

    private int rayCount;
    private float[] wallDistances;
    private bool[] wallHit;

    private Texture2D sonarTexture;
    private Color32[] pixelBuffer; 
    private int centerPixel;

    void Awake() { Instance = this; }

    void Start()
    {
        RectTransform rt = sonarDisplayImage.GetComponent<RectTransform>();
        radarUIRadius = rt.rect.width / 2f;

        // 解像度に基づいて配列を初期化（scanResolution = 0.5 なら 720本のレイ）
        rayCount = Mathf.Max(1, Mathf.CeilToInt(360f / scanResolution));
        wallDistances = new float[rayCount];
        wallHit = new bool[rayCount];

        mainPulseRing = CreateAutoRing("Auto_MainPulseRing", mainPulseColor, 2f, false);
        noiseRangeCircle = CreateAutoRing("Auto_NoiseRangeCircle", noiseRangeColor, 0f, true); 
        missionPulseRing = CreateAutoRing("Auto_MissionPulseRing", missionPulseColor, 2f, false);

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
                    case SubmarineTargetType.Mine: blipImage.color = Color.red; break;
                    case SubmarineTargetType.HostileBio: blipImage.color = new Color(1f, 0.4f, 0f); break;
                    case SubmarineTargetType.NeutralBio: blipImage.color = Color.cyan; break;
                    case SubmarineTargetType.Item: blipImage.color = Color.yellow; break;
                    case SubmarineTargetType.Objective:
                        blipImage.color = new Color(0f, 1f, 0f, 0.4f); 
                        float uiSize = Mathf.Max((target.areaRadius / sonarRange) * 300f, 30f);
                        newBlip.GetComponent<RectTransform>().sizeDelta = new Vector2(uiSize, uiSize); 
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

        sonarTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        pixelBuffer = new Color32[textureSize * textureSize];
        Color32 clearColor = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixelBuffer.Length; i++) pixelBuffer[i] = clearColor;
        sonarTexture.SetPixels32(pixelBuffer);
        sonarTexture.Apply();
        sonarDisplayImage.texture = sonarTexture;
        centerPixel = textureSize / 2;

        if (missionMarker != null) missionMarker.gameObject.SetActive(false);
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

    public void SetMissionTarget(Transform target)
    {
        currentMissionTarget = target;
        if (missionMarker != null) missionMarker.gameObject.SetActive(target != null);

        if (target == null && missionPulseRing != null)
        {
            missionPulseAlpha = 0f;
            Color c = missionPulseRing.color;
            c.a = 0f;
            missionPulseRing.color = c;
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateNoiseRing();
        FadeOutPixels();

        prevPulseDistance = currentPulseDistance;
        currentPulseDistance += pulseScanSpeed * Time.deltaTime;

        if (mainPulseRing != null)
        {
            float pulseUIRadius = (currentPulseDistance / sonarRange) * radarUIRadius;
            mainPulseRing.rectTransform.sizeDelta = new Vector2(pulseUIRadius * 2f, pulseUIRadius * 2f);

            Color c = mainPulseRing.color;
            c.a = Mathf.Clamp01(1.0f - (currentPulseDistance / sonarRange));
            mainPulseRing.color = c;
        }

        DrawPulseEchoes();
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

        for (int i = 0; i < rayCount; i++)
        {
            float angleDeg = i * scanResolution;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad));
            direction = Quaternion.Euler(0, player.eulerAngles.y, 0) * direction;
            Vector3 rayOrigin = player.position + new Vector3(0, 1.0f, 0);

            if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, sonarRange, wallLayer))
            {
                wallHit[i] = true;
                wallDistances[i] = hit.distance;
            }
            else wallHit[i] = false;
        }
    }

    
    private void DrawPulseEchoes()
    {
        bool textureUpdated = false;

        for (int i = 0; i < rayCount; i++)
        {
            if (wallHit[i] && wallDistances[i] > prevPulseDistance && wallDistances[i] <= currentPulseDistance)
            {
                float r0 = (wallDistances[i] / sonarRange) * centerPixel;
                float maxR = centerPixel;
                
                float angleDeg = i * scanResolution;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                for (float radius = r0; radius < maxR; radius += 0.5f)
                {
                    float t = (radius - r0) / (maxR - r0);
                    Color32 drawColor = shadowGradient.Evaluate(t);

                    int px = Mathf.RoundToInt(centerPixel + Mathf.Sin(angleRad) * radius);
                    int py = Mathf.RoundToInt(centerPixel + Mathf.Cos(angleRad) * radius);

                    if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                    {
                        pixelBuffer[py * textureSize + px] = drawColor;
                        textureUpdated = true;
                    }
                }
            }
        }

        if (textureUpdated)
        {
            sonarTexture.SetPixels32(pixelBuffer);
            sonarTexture.Apply();
        }
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
                if (blips[i] != null && blips[i].activeSelf) blips[i].SetActive(false);
                continue; 
            }

            Vector3 relativePos = targets[i].transform.position - player.position;
            float distance = new Vector2(relativePos.x, relativePos.z).magnitude;

            if (distance <= sonarRange)
            {
                UnityEngine.UI.Image blipImage = blips[i].GetComponent<UnityEngine.UI.Image>();

                if (distance > prevPulseDistance && distance <= currentPulseDistance) 
                {
                    blips[i].SetActive(true);
                    if (blipImage != null)
                    {
                        Color c = blipImage.color;
                        c.a = 1f; 
                        blipImage.color = c;
                    }

                    float angle = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
                    angle -= player.eulerAngles.y;
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

    private void UpdateMissionNavigation()
    {
        if (currentMissionTarget == null || missionMarker == null) return;

        Vector3 dir = currentMissionTarget.position - player.position;
        float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float relativeAngle = targetAngle - player.eulerAngles.y;
        float angleRad = relativeAngle * Mathf.Deg2Rad;

        float markerX = Mathf.Sin(angleRad) * radarUIRadius;
        float markerY = Mathf.Cos(angleRad) * radarUIRadius;
        
        missionMarker.localPosition = new Vector3(markerX, markerY, 0);
        missionMarker.localEulerAngles = new Vector3(0, 0, -relativeAngle);

        float distance = new Vector2(dir.x, dir.z).magnitude;
        
        if (distance <= pulseTriggerDistance)
        {
            missionPulseTimer += Time.deltaTime;
            float currentInterval = Mathf.Lerp(0.3f, missionPulseInterval, distance / pulseTriggerDistance);

            if (missionPulseTimer >= currentInterval)
            {
                missionPulseTimer = 0f;
                float distanceRatio = Mathf.Clamp01(distance / sonarRange);
                float uiX = Mathf.Sin(angleRad) * distanceRatio * radarUIRadius;
                float uiY = Mathf.Cos(angleRad) * distanceRatio * radarUIRadius;

                if (missionPulseRing != null)
                {
                    missionPulseRing.rectTransform.localPosition = new Vector3(uiX, uiY, 0); 
                    missionPulseRing.rectTransform.sizeDelta = Vector2.zero; 
                    missionPulseAlpha = 1f; 
                }

                if (sonarAudioSource != null && pulseSound != null)
                {
                    sonarAudioSource.PlayOneShot(pulseSound, 0.7f);
                }
            }
        }

        if (missionPulseRing != null && missionPulseAlpha > 0f)
        {
            missionPulseRing.rectTransform.sizeDelta += new Vector2(pulseExpandSpeed, pulseExpandSpeed) * Time.deltaTime;
            missionPulseAlpha -= pulseFadeSpeed * Time.deltaTime;

            Color c = missionPulseRing.color;
            c.a = Mathf.Max(0f, missionPulseAlpha);
            missionPulseRing.color = c;
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
}

// UIRingクラスはそのまま（変更なし）
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