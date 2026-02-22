using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Message UI")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Menu UI")]
    [SerializeField] private GameObject menuBackgroundPanel; // 共通の背景パネル
    [SerializeField] private TextMeshProUGUI menuTitleText;  // 共通の見出しテキスト
    [SerializeField] private GameObject mainPage;            // メイン画面の入れ物
    [SerializeField] private GameObject inventoryPage;       // もちもの画面の入れ物

    [SerializeField] private GameObject missionPage;         
    [SerializeField] private MissionMenuUI missionUI;

    [Header("Player Control")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Interact UI")]
    [SerializeField] private GameObject interactPrompt;

    [Tooltip("画面中央のクロスヘアUI")]
    public GameObject crosshair;


    private bool isMenuOpen = false;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    public bool canOpenMenu = true;

    private void Update()
    {
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

    public void ShowMessage(string text)
    {
        messageText.text = text;
        messagePanel.SetActive(true);

        // もし既にタイマーが動いていたら、一度リセットする（連続でアイテムを拾った時におかしくならないようにするため）
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        
        // 3秒後に消すタイマーをスタート！（秒数は 3f の部分でお好みで調整できます）
        hideCoroutine = StartCoroutine(HideMessageAfterDelay(3f));
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // 指定した秒数（今回は3秒）だけ待機する
        HideMessage(); // 待機が終わったら、元々ある「隠すメソッド」を呼ぶ
    }

    public void HideMessage()
    {
        messagePanel.SetActive(false);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null; // リセット
        }
    }

    // メニュー全体の開閉処理
    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuBackgroundPanel.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
            // メニューを開いた時は、必ず「メインページ」からスタートする
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

    // メインページを表示するメソッド
    public void OpenMainPage()
    {
        menuTitleText.text = "MAIN MENU"; // 見出しを変更
        mainPage.SetActive(true);         // メインの中身を表示
        inventoryPage.SetActive(false);   // インベントリの中身を隠す
        if (missionPage != null) missionPage.SetActive(false);
    }

    // インベントリページを表示するメソッド（ボタンから呼ばれる）
    public void OpenInventoryPage()
    {
        menuTitleText.text = "もちもの";  // 見出しを変更
        mainPage.SetActive(false);        // メインの中身を隠す
        inventoryPage.SetActive(true);    // インベントリの中身を表示

        if (missionPage != null) missionPage.SetActive(false);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearItemDetail();
        }
    }

    public void OpenMissionPage()
    {
        menuTitleText.text = "現在の目的"; 
        mainPage.SetActive(false);       
        inventoryPage.SetActive(false);  
        if (missionPage != null) missionPage.SetActive(true);  // 目的の中身を表示

        // ページを開いた瞬間に、GameManagerから最新のフラグ状態を読み取ってテキストを更新する
        if (missionUI != null)
        {
            missionUI.UpdateMissionUI();
        }
    }

    public void ShowInteractPrompt(string promptText)
    {
        // 受け取った文字が空っぽ（""）じゃなければ表示する
        if (!string.IsNullOrEmpty(promptText))
        {
            // UIのテキストを書き換える
            interactPrompt.GetComponent<TextMeshProUGUI>().text = "[E] " + promptText;
            interactPrompt.SetActive(true);
        }
        else
        {
            // 空っぽなら非表示にする
            interactPrompt.SetActive(false);
        }
    }

    [Header("Mission Notification UI")]
    [SerializeField] private RectTransform notificationPanel; // 右から出るパネル本体
    [SerializeField] private TextMeshProUGUI notificationText; // 「目的が更新されました」等の文字
    
    [Tooltip("画面外に隠れている時のX座標（右側）")]
    [SerializeField] private float hideXPosition = 400f; 
    [Tooltip("画面内に出た時のX座標")]
    [SerializeField] private float showXPosition = -20f; 
    
    private Coroutine notificationCoroutine;

    // ==========================================
    // ニョキッと出る通知処理
    // ==========================================
    public void ShowMissionNotification(string message)
    {
        if (notificationPanel == null) return;

        notificationText.text = message;

        // すでに通知が出ている途中なら、一度リセットする
        if (notificationCoroutine != null) StopCoroutine(notificationCoroutine);
        
        notificationPanel.gameObject.SetActive(true);
        notificationCoroutine = StartCoroutine(SlideNotification());
    }

    private IEnumerator SlideNotification()
    {
        float timer = 0f;
        float duration = 0.5f; // スライドにかかる時間（0.5秒）

        // ① ニョキッと左へスライド（出現）
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration); // 滑らかな動き
            float currentX = Mathf.Lerp(hideXPosition, showXPosition, t);
            notificationPanel.anchoredPosition = new Vector2(currentX, notificationPanel.anchoredPosition.y);
            yield return null;
        }

        // ② 3秒間そのまま待機
        yield return new WaitForSeconds(3f);

        // ③ 右へスライドして戻る（消える）
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
}