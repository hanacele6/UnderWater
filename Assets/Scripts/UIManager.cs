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

    [Header("Player Control")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Interact UI")]
    [SerializeField] private GameObject interactPrompt;

    private bool isMenuOpen = false;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // TABキーでメニュー全体の開閉
        if (Input.GetKeyDown(KeyCode.Tab))
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
    }

    // インベントリページを表示するメソッド（ボタンから呼ばれる）
    public void OpenInventoryPage()
    {
        menuTitleText.text = "もちもの";  // 見出しを変更
        mainPage.SetActive(false);        // メインの中身を隠す
        inventoryPage.SetActive(true);    // インベントリの中身を表示

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearItemDetail();
        }
    }

    public void ShowInteractPrompt(bool isShowing)
    {
        interactPrompt.SetActive(isShowing);
    }
}