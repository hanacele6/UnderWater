using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TacticalMapSystem : MonoBehaviour
{
    public static TacticalMapSystem Instance;

    [Header("カメラ＆レンダリング")]
    public Camera mapCamera;
    
    [Header("UI 参照")]
    public GameObject mapPanel;
    public CanvasGroup mapCanvasGroup;
    public RectTransform mapContent;
    public TMP_Text sonarSectorText; 

    //public GameObject mapCanvas;

    [Header("マップ操作設定")]
    public Transform player;
    public float gridSize = 100f; 
    public KeyCode openKey = KeyCode.M;
    public float minZoom = 50f;
    public float maxZoom = 500f;
    public float zoomSpeed = 30f;

    [Tooltip("壁を見下ろせるカメラの固定の高さ（Y座標）")]
    public float cameraHeight = 10500f; 
    [Tooltip("中心からドラッグで移動できる最大距離")]
    public float maxPanDistance = 3000f;

    [Header("動的UIプレハブ")]
    public RectTransform playerIcon; 
    public GameObject gridLinePrefab; 
    public GameObject gridTextPrefab; 

    private bool isOpen = false;
    private Coroutine transitionCoroutine;
    private Vector3 cameraOffset;
    private Vector3 lastMousePos;
    
    private bool isDraggingMap = false;

    private class GridLineUI
    {
        public RectTransform line;
        public TMP_Text label;
        public Vector3 worldPos;
        public bool isVertical;
    }
    private List<GridLineUI> activeGridLines = new List<GridLineUI>();

    void Awake() { Instance = this; }

    void Start()
    {
        if (mapPanel != null) mapPanel.SetActive(false);
        GenerateGridUI();
        
    }

    void Update()
    {
        UpdateSectorDisplay();

        if (Input.GetKeyDown(openKey)) ToggleMap();

        if (isOpen)
        {
            UpdateMapCamera();
            UpdateMapUI();
        }
    }

    public void ToggleMap()
    {
        isOpen = !isOpen;
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        if (isOpen)
        {
            mapPanel.SetActive(true);
            
            if (mapCanvasGroup != null)
            {
                mapCanvasGroup.interactable = true;
                mapCanvasGroup.blocksRaycasts = true;
            }


            if (CargoPhysicsUI.Instance != null && CargoPhysicsUI.Instance.containerDropArea != null) 
            {
                CargoPhysicsUI.Instance.containerDropArea.gameObject.SetActive(false);
            }

            cameraOffset = Vector3.zero; 
            UpdateMapCamera(); 
            UpdateMapUI();
            
            transitionCoroutine = StartCoroutine(OpenMapAnimation());
            if (SubmarineHUD.Instance != null) SubmarineHUD.Instance.AddLog("システム: 戦術マップ起動...", "#88FFFF");
            if (GameManager.Instance != null) GameManager.Instance.LockPlayer();
        }
        else
        {
            if (mapCanvasGroup != null) mapCanvasGroup.blocksRaycasts = false;

            if (CargoPhysicsUI.Instance != null && CargoPhysicsUI.Instance.containerDropArea != null) 
            {
                CargoPhysicsUI.Instance.containerDropArea.gameObject.SetActive(true);
            }
            
            transitionCoroutine = StartCoroutine(CloseMapAnimation());
            //if (GameManager.Instance != null) GameManager.Instance.UnlockPlayer();
        }
    }

    private void UpdateMapCamera()
    {
        if (player == null || mapCamera == null) return;

        if (Input.GetMouseButtonDown(0))
        {

            lastMousePos = Input.mousePosition;
            isDraggingMap = true; 
        }
        else if (Input.GetMouseButton(0) && isDraggingMap)
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            float panFactor = mapCamera.orthographicSize / Screen.height * 2f;
            cameraOffset -= new Vector3(delta.x * panFactor, 0, delta.y * panFactor);
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDraggingMap = false;
        }

        cameraOffset.x = Mathf.Clamp(cameraOffset.x, -maxPanDistance, maxPanDistance);
        cameraOffset.z = Mathf.Clamp(cameraOffset.z, -maxPanDistance, maxPanDistance);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            mapCamera.orthographicSize = Mathf.Clamp(mapCamera.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
        }

        mapCamera.transform.position = new Vector3(player.position.x, cameraHeight, player.position.z) + cameraOffset;
    }

    private void UpdateMapUI()
    {
        if (player == null || mapCamera == null || mapContent == null) return;

        if (playerIcon != null)
        {
            playerIcon.localPosition = WorldToMapUI(player.position);
            playerIcon.localEulerAngles = new Vector3(0, 0, -player.eulerAngles.y);
        }

        foreach (var grid in activeGridLines)
        {
            Vector2 uiPos = WorldToMapUI(grid.worldPos);
            
            if (grid.isVertical)
            {
                grid.line.localPosition = new Vector3(uiPos.x, 0, 0);
                grid.line.sizeDelta = new Vector2(2f, mapContent.rect.height); 
                if (grid.label != null) grid.label.rectTransform.localPosition = new Vector3(uiPos.x + 10f, mapContent.rect.height / 2f - 20f, 0);
            }
            else
            {
                grid.line.localPosition = new Vector3(0, uiPos.y, 0);
                grid.line.sizeDelta = new Vector2(mapContent.rect.width, 2f);
                if (grid.label != null) grid.label.rectTransform.localPosition = new Vector3(-mapContent.rect.width / 2f + 20f, uiPos.y + 10f, 0);
            }
        }
    }

    public Vector2 WorldToMapUI(Vector3 worldPos)
    {
        Vector3 viewportPos = mapCamera.WorldToViewportPoint(worldPos);
        return new Vector2(
            (viewportPos.x - 0.5f) * mapContent.rect.width,
            (viewportPos.y - 0.5f) * mapContent.rect.height
        );
    }

    private void GenerateGridUI()
    {
        if (gridLinePrefab == null || gridTextPrefab == null) return;

        for (int i = -50; i <= 50; i++)
        {
            float pos = i * gridSize;
            CreateGridLine(new Vector3(pos, 0, 0), true, GetColName(i));
            CreateGridLine(new Vector3(0, 0, pos), false, GetRowName(i));
        }
    }

    private void CreateGridLine(Vector3 wPos, bool isVert, string text)
    {
        GameObject lineObj = Instantiate(gridLinePrefab, mapContent);
        GameObject textObj = Instantiate(gridTextPrefab, mapContent);
        
        Image img = lineObj.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);
        img.raycastTarget = false; 

        TMP_Text tmp = textObj.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.color = new Color(1f, 1f, 1f, 0.3f);
        tmp.fontSize = 18;
        tmp.raycastTarget = false; 

        activeGridLines.Add(new GridLineUI {
            line = lineObj.GetComponent<RectTransform>(),
            label = tmp,
            worldPos = wPos,
            isVertical = isVert
        });
    }

    private void UpdateSectorDisplay()
    {
        if (player == null || sonarSectorText == null) return;
        sonarSectorText.text = $"SECTOR\n<size=150%>{GetSectorString(player.position)}</size>";
    }

    public string GetSectorString(Vector3 pos)
    {
        int xIndex = Mathf.FloorToInt(pos.x / gridSize);
        int zIndex = Mathf.FloorToInt(pos.z / gridSize);
        string col = GetColName(xIndex);
        int row = zIndex + 51;
        return $"{col}-{row}";
    }

    private string GetColName(int index) 
    { 
        int n = index + 51; 
        string result = "";
        while (n > 0)
        {
            n--;
            result = (char)('A' + (n % 26)) + result;
            n /= 26;
        }
        return result;
    }

    private string GetRowName(int index) { return (index + 51).ToString(); }

    private IEnumerator OpenMapAnimation()
    {
        mapCanvasGroup.alpha = 0f;
        mapContent.localScale = new Vector3(1.05f, 0.01f, 1f); 

        float t = 0;
        while (t < 0.1f) { t += Time.deltaTime; mapCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.1f); yield return null; }

        t = 0;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float ease = 1f + 2.70158f * Mathf.Pow(t / 0.25f - 1f, 3f) + 1.70158f * Mathf.Pow(t / 0.25f - 1f, 2f);
            mapContent.localScale = new Vector3(1f, Mathf.Lerp(0.01f, 1f, ease), 1f);
            yield return null;
        }
        mapContent.localScale = Vector3.one;
        mapCanvasGroup.alpha = 1f;
    }

    private IEnumerator CloseMapAnimation()
    {
        mapContent.localScale = new Vector3(1f, 0.02f, 1f);
        yield return new WaitForSeconds(0.08f);
        mapCanvasGroup.alpha = 0f;
        mapPanel.SetActive(false);
    }
}