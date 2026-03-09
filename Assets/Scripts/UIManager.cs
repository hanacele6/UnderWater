using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic; // Listを使うために追加
using System;                     // Action（コールバック）を使うために追加

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Message UI")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Menu UI")]
    [SerializeField] private GameObject menuBackgroundPanel;
    [SerializeField] private TextMeshProUGUI menuTitleText;
    [SerializeField] private GameObject mainPage;
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private GameObject missionPage;
    [SerializeField] private MissionMenuUI missionUI;

    [Header("Player Control")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Interact UI")]
    [SerializeField] private GameObject interactPrompt;
    [Tooltip("画面中央のクロスヘアUI")]
    public GameObject crosshair;

    [Header("Mission Notification UI")]
    [SerializeField] private RectTransform notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float hideXPosition = 400f;
    [SerializeField] private float showXPosition = -20f;
    
    // ==========================================
    // 常時表示メインミッション＆会話UI
    // ==========================================
    [Header("Main Mission HUD")]
    [SerializeField] private GameObject mainMissionPanel;       // 画面左上などに常時出すパネル（任意）
    [SerializeField] private TextMeshProUGUI mainMissionText;   // 「現在の目標：〇〇」を表示するテキスト

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;          // 会話ウィンドウ全体
    [SerializeField] private TextMeshProUGUI speakerNameText;   // 話者名（「艦長」など）
    [SerializeField] private TextMeshProUGUI dialogueMessageText; // セリフ本文

    

    private bool isMenuOpen = false;
    private Coroutine hideCoroutine;
    private Coroutine notificationCoroutine;

    // 会話用の状態管理変数
    private bool isDialogueActive = false;
    private List<DialogueLine> currentDialogueLines;
    private int currentLineIndex = 0;
    private Action onDialogueComplete;

    [Header("Fade UI")]
    [Tooltip("画面全体を覆う真っ黒なパネル")]
    public CanvasGroup fadeCanvasGroup;

    private void Awake()
    {
        Instance = this;
        
        // 最初は会話パネルを隠しておく
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    public bool canOpenMenu = true;

    private void Update()
    {
        // 会話中はメニューを開けないようにし、クリックで会話を進める
        if (isDialogueActive)
        {
            // 左クリックで次のセリフへ
            if (Input.GetMouseButtonDown(0))
            {
                OnDialogueNextClicked();
            }
            return; // 会話中はこれ以下の処理（メニュー開閉など）を無視する
        }

        // TABキーでメニュー全体の開閉
        if (canOpenMenu && Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMenu();
        }

        if (messagePanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            HideMessage();
        }
    }

    // ==========================================
    // 今回追加：メインミッションの更新処理
    // ==========================================
    public void UpdateMainMission(string newObjective)
    {
        if (mainMissionText != null)
        {
            mainMissionText.text = newObjective;
            if (mainMissionPanel != null) mainMissionPanel.SetActive(true);
        }

        // ついでに右からのスライド通知も出すと親切です
        ShowMissionNotification("目的が更新されました");
    }

    // ==========================================
    // 今回追加：会話（Dialogue）システム
    // ==========================================
    public void StartDialogue(List<DialogueLine> lines, Action onCompleteCallback)
    {
        if (lines == null || lines.Count == 0)
        {
            onCompleteCallback?.Invoke(); // セリフが空なら即終了
            return;
        }

        isDialogueActive = true;
        currentDialogueLines = lines;
        currentLineIndex = 0;
        onDialogueComplete = onCompleteCallback;

        // UI表示を切り替え
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (crosshair != null) crosshair.SetActive(false); // 会話中はクロスヘアを消す

        ShowNextDialogueLine();
    }

    private void ShowNextDialogueLine()
    {
        DialogueLine line = currentDialogueLines[currentLineIndex];
        if (speakerNameText != null) speakerNameText.text = line.speakerName;
        if (dialogueMessageText != null) dialogueMessageText.text = line.message;
    }

    public void OnDialogueNextClicked()
    {
        currentLineIndex++;
        if (currentLineIndex < currentDialogueLines.Count)
        {
            // まだセリフが残っていれば次を表示
            ShowNextDialogueLine();
        }
        else
        {
            // 全て読み終わったら終了処理へ
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (crosshair != null) crosshair.SetActive(true);

        // GameManagerに「会話終わったよ！」と伝える
        onDialogueComplete?.Invoke(); 
    }

    // ==========================================
    // 以下、既存のメッセージ・メニュー・通知処理（変更なし）
    // ==========================================
    public void ShowMessage(string text)
    {
        messageText.text = text;
        messagePanel.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideMessageAfterDelay(3f));
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideMessage();
    }

    public void HideMessage()
    {
        messagePanel.SetActive(false);
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuBackgroundPanel.SetActive(isMenuOpen);

        SetMainMissionPanelVisible(!isMenuOpen);

        if (isMenuOpen)
        {
            HideMessage();
            OpenMainPage();

            playerInput.enabled = false; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            playerInput.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OpenMainPage()
    {
        menuTitleText.text = "MAIN MENU"; 
        mainPage.SetActive(true);        
        inventoryPage.SetActive(false);   
        if (missionPage != null) missionPage.SetActive(false);
    }

    public void OpenInventoryPage()
    {
        menuTitleText.text = "もちもの";  
        mainPage.SetActive(false);        
        inventoryPage.SetActive(true);    
        if (missionPage != null) missionPage.SetActive(false);

        // ※InventoryManagerのエラー回避のため、存在チェックのみ残します
        // もしClearItemDetailでエラーが出る場合は適宜修正してください。
    }

    public void OpenMissionPage()
    {
        menuTitleText.text = "現在の目的"; 
        mainPage.SetActive(false);       
        inventoryPage.SetActive(false);  
        if (missionPage != null) missionPage.SetActive(true);  

        if (missionUI != null) missionUI.UpdateMissionUI();
    }

    public void ShowInteractPrompt(string promptText)
    {
        if (!string.IsNullOrEmpty(promptText))
        {
            interactPrompt.GetComponent<TextMeshProUGUI>().text = "[E] " + promptText;
            interactPrompt.SetActive(true);
        }
        else
        {
            interactPrompt.SetActive(false);
        }
    }

    public void ShowMissionNotification(string message)
    {
        if (notificationPanel == null) return;

        notificationText.text = message;

        if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);
        
        notificationPanel.gameObject.SetActive(true);
        notificationCoroutine = StartCoroutine(SlideNotification());
    }

    private IEnumerator SlideNotification()
    {
        float timer = 0f;
        float duration = 0.5f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            float currentX = Mathf.Lerp(hideXPosition, showXPosition, t);
            notificationPanel.anchoredPosition = new Vector2(currentX, notificationPanel.anchoredPosition.y);
            yield return null;
        }

        yield return new WaitForSeconds(3f);

        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            float currentX = Mathf.Lerp(showXPosition, hideXPosition, t);
            notificationPanel.anchoredPosition = new Vector2(currentX, notificationPanel.anchoredPosition.y);
            yield return null;
        }

        notificationPanel.gameObject.SetActive(false);
    }

    // HUDの表示・非表示を切り替えるメソッド
    public void SetMainMissionPanelVisible(bool isVisible)
    {
        if (mainMissionPanel != null)
        {
            // ただし「表示しろ」と言われても、目的のテキストが空っぽなら枠だけ出さないようにする
            if (isVisible && !string.IsNullOrEmpty(mainMissionText.text))
            {
                mainMissionPanel.SetActive(true);
            }
            else
            {
                mainMissionPanel.SetActive(false);
            }
        }
    }

    // 画面を暗くする（フェードアウト）
    public IEnumerator FadeOut(float duration)
    {
        if (fadeCanvasGroup == null) yield break;
        fadeCanvasGroup.gameObject.SetActive(true);
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    // 画面を明るくする（フェードイン）
    public IEnumerator FadeIn(float duration)
    {
        if (fadeCanvasGroup == null) yield break;
        
        fadeCanvasGroup.gameObject.SetActive(true); 

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(t / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.gameObject.SetActive(false);
    }
}