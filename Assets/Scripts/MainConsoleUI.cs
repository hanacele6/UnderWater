using UnityEngine;
using UnityEngine.UI;

public class MainConsoleUI : MonoBehaviour
{
    public static MainConsoleUI Instance { get; private set; }

    [Header("大枠パネル")]
    public GameObject mainContainer;

    [Header("各タブの画面（パネル）")]
    public GameObject panelTree;
    public GameObject panelEquip;
    public GameObject panelMission;

    [Header("タブ切り替えボタン")]
    public Button tabButtonTree;
    public Button tabButtonEquip;
    public Button tabButtonMission;

    [Header("閉じるボタン")]
    public Button closeButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // ボタンが押された時の処理を登録
        tabButtonTree.onClick.AddListener(() => ShowTab(0));
        tabButtonEquip.onClick.AddListener(() => ShowTab(1));
        tabButtonMission.onClick.AddListener(() => ShowTab(2));

        closeButton.onClick.AddListener(CloseUI);

        // 最初は閉じておく
        if (mainContainer != null) mainContainer.SetActive(false);
    }

    // MainConsole.csのInteract()から呼ばれる
    public void OpenUI()
    {
        mainContainer.SetActive(true);
        ShowTab(0); // 開いた時は必ず最初のタブを表示

        // プレイヤーの操作をロック
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isUIOpen = true;
            GameManager.Instance.LockPlayer();
        }
    }

    public void CloseUI()
    {
        mainContainer.SetActive(false);

        // プレイヤーの操作を戻す
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isUIOpen = false;
            GameManager.Instance.UnlockPlayer();
        }
    }

    // タブの切り替え処理
    private void ShowTab(int tabIndex)
    {
        // 選ばれたタブだけを true にし、他を false にする
        panelTree.SetActive(tabIndex == 0);
        panelEquip.SetActive(tabIndex == 1);
        panelMission.SetActive(tabIndex == 2);

        // 💡 必要に応じて、タブが開かれた時に中身のデータを更新する処理を呼ぶ
        // if (tabIndex == 0) RefreshTreeUI();
        // if (tabIndex == 1) RefreshEquipUI();
        // if (tabIndex == 2) RefreshMissionUI();
    }
}