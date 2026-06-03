using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainConsoleUI : MonoBehaviour
{
    public static MainConsoleUI Instance;

    [Header("パネル・コンテナ")]
    public GameObject consolePanel; 
    public GameObject[] tabPanels; // 0:Upgrade, 1:Equip, 2:Mission

    [Header("タブボタン")]
    public Button upgradeTabBtn;
    public Button equipTabBtn;
    public Button missionTabBtn;
    public Button closeBtn;

    [Header("【ミッション提出タブ】のUI要素")]
    public TextMeshProUGUI missionTitleText;
    public TextMeshProUGUI requirementText; // 今回はシンプルに1つのテキストにまとめて表示します
    public Button submitBtn;
    public TextMeshProUGUI warningText;

    private void Awake()
    {
        Instance = this;
        
        upgradeTabBtn.onClick.AddListener(() => SwitchTab(0));
        equipTabBtn.onClick.AddListener(() => SwitchTab(1));
        missionTabBtn.onClick.AddListener(() => SwitchTab(2));
        closeBtn.onClick.AddListener(CloseConsole);

        // 💡 提出ボタンが押された時の処理を登録
        submitBtn.onClick.AddListener(OnSubmitClicked);

        consolePanel.SetActive(false);
    }

    public void OpenConsole()
    {
        consolePanel.SetActive(true);
        SwitchTab(2); // ミッションタブを開く
        
        GameManager.Instance.isUIOpen = true;
        GameManager.Instance.LockPlayer();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetInteractUIVisible(false);
            UIManager.Instance.SetDialogueMode(true); 
            UIManager.Instance.SetHUDVisible(false); 
        }
    }

    public void CloseConsole()
    {
        consolePanel.SetActive(false);
        
        GameManager.Instance.isUIOpen = false;
        GameManager.Instance.UnlockPlayer();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetInteractUIVisible(true);
            UIManager.Instance.SetDialogueMode(false); 
            UIManager.Instance.SetHUDVisible(true); 
        }
    }

    private void SwitchTab(int index)
    {
        for (int i = 0; i < tabPanels.Length; i++)
        {
            tabPanels[i].SetActive(i == index);
        }

        // タブが開かれたら画面を最新状態に更新する
        if (index == 2)
        {
            RefreshMissionTab();
        }
        else if (index == 0) // 💡 ツリータブ（インデックス0）が開かれた時
        {
            // Panel_UpgradeTree に付けた UpgradeTreeUI を取得して初期化
            UpgradeTreeUI treeUI = tabPanels[0].GetComponent<UpgradeTreeUI>();
            if (treeUI != null) treeUI.InitializeTree();
        }
        else if (index == 1) 
        {
            EquipmentUI equipUI = tabPanels[1].GetComponent<EquipmentUI>();
            if (equipUI != null) equipUI.InitializeEquipTab();
        }
    }

    // 💡 ミッション画面の表示をインベントリの状況に合わせて更新する処理
    public void RefreshMissionTab()
    {
        if (MainConsole.Instance == null) return;

        // 💡 GameManagerと同期した「現在アクティブな提出ミッション」を1発で取得！
        var currentMission = MainConsole.Instance.GetCurrentActiveSubmissionMission();

        if (currentMission == null)
        {
            missionTitleText.text = "現在、提出可能な任務はありません";
            requirementText.text = "無線システムやメニューから次の指示を確認してください。";
            warningText.text = "<color=white>【待機中】</color>";
            submitBtn.interactable = false;
            return;
        }

        missionTitleText.text = currentMission.displayText;

        // 要求素材のテキストを組み立てる
        string reqString = "【必要素材】\n";
        bool isAllAvailable = true;

        foreach (var req in currentMission.requiredItems)
        {
            int currentCount = InventoryManager.Instance.GetItemCount(req.item);
            
            if (currentCount >= req.amount)
            {
                reqString += $"<color=green>✓ {req.item.itemName} ({currentCount} / {req.amount})</color>\n";
            }
            else
            {
                reqString += $"<color=red>✗ {req.item.itemName} ({currentCount} / {req.amount})</color>\n";
                isAllAvailable = false; 
            }
        }

        requirementText.text = reqString;
        
        // 💡 ボタンの有効・無効化と同時に、WarningTextに現在のステータスを出す
        submitBtn.interactable = isAllAvailable;

        if (isAllAvailable)
        {
            warningText.text = "<color=yellow>⚠️ 素材が揃っています。提出可能です。</color>";
        }
        else
        {
            warningText.text = "<color=orange>⏳ 潜水艦内または海中から素材を回収してください。</color>";
        }
    }

    private void OnSubmitClicked()
    {
        if (MainConsole.Instance == null) return;

        var currentMission = MainConsole.Instance.GetCurrentActiveSubmissionMission();
        if (currentMission == null) return;

        bool success = MainConsole.Instance.TrySubmitMission(currentMission);

        if (success)
        {
            Debug.Log("UI：ミッション提出成功！");
            
            // 💡 提出が完了したら、WarningTextを「完了」に変える
            warningText.text = "<color=green>🎉 提出が完了し、システムが更新されました！</color>";
            
            // 画面をリフレッシュ（次のフェーズの任務があれば切り替わり、無ければ待機中になります）
            RefreshMissionTab(); 
        }
    }
}