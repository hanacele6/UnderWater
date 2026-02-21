using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq; 

public class MissionMenuUI : MonoBehaviour
{
    [Header("UI References - Lists")]
    public Transform mainMissionContainer; // メイン任務リストが入る箱
    public Transform subMissionContainer;  // サブ任務リストが入る箱
    public GameObject missionButtonPrefab; // インベントリと同じようなボタンプレハブ

    [Header("UI References - Details")]
    public TextMeshProUGUI detailTitleText;       // 右側の見出しテキスト
    public TextMeshProUGUI detailDescriptionText; // 右側の詳細テキスト

    // 折りたたみの状態記憶（最初は両方開いておく）
    private bool isMainExpanded = true;
    private bool isSubExpanded = true;

    void OnEnable()
    {
        UpdateMissionUI();
    }

    // ==========================================
    // ヘッダーボタンから呼ばれる開閉メソッド
    // ==========================================
    public void ToggleMainMissions()
    {
        isMainExpanded = !isMainExpanded;
        mainMissionContainer.gameObject.SetActive(isMainExpanded);
        // ※LayoutGroupが自動で隙間を詰めてくれます
    }

    public void ToggleSubMissions()
    {
        isSubExpanded = !isSubExpanded;
        subMissionContainer.gameObject.SetActive(isSubExpanded);
    }

    // ==========================================
    // UIの生成・更新
    // ==========================================
    public void UpdateMissionUI()
    {
        if (GameManager.Instance == null) return;

        // 1. 古いボタンをすべて消去
        foreach (Transform child in mainMissionContainer) Destroy(child.gameObject);
        foreach (Transform child in subMissionContainer) Destroy(child.gameObject);

        ClearMissionDetail();

        int currentDay = GameManager.Instance.currentDay;
        GamePhase currentPhase = GameManager.Instance.currentPhase;

        // 2. 表示条件を満たすミッションだけを LINQ で抽出
        List<GameManager.MissionObjective> displayMissions = GameManager.Instance.missionList
            .Where(m => (m.appearDay == 0 || m.appearDay == currentDay) && currentPhase >= m.appearPhase)
            .ToList();

        // 3. ボタンを生成して、それぞれの箱に振り分ける
        foreach (var mission in displayMissions)
        {
            // メインかサブかで、入れる親（Transform）を変える！
            Transform targetContainer = mission.isMainObjective ? mainMissionContainer : subMissionContainer;
            
            GameObject newButton = Instantiate(missionButtonPrefab, targetContainer);

            // テキストの設定（クリア済みなら取り消し線とグレーアウト）
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            bool isCleared = GameManager.Instance.GetFlag(mission.targetFlagName);

            if (buttonText != null)
            {
                buttonText.text = isCleared ? $"<s>{mission.displayText}</s>" : mission.displayText;
                if (isCleared) buttonText.color = Color.gray; 
            }

            // ボタンをクリックした時の処理を登録
            Button btn = newButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowMissionDetail(mission, isCleared));
            }
        }
    }

    // ==========================================
    // 詳細画面の表示処理
    // ==========================================
    public void ShowMissionDetail(GameManager.MissionObjective mission, bool isCleared)
    {
        if (detailTitleText != null) detailTitleText.text = mission.displayText;

        string statusText = isCleared ? "<color=#88FF88>【達成済み】</color>\n\n" : "<color=#FFCC00>【進行中】</color>\n\n";
        
        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = statusText + mission.description;
        }
    }

    public void ClearMissionDetail()
    {
        if (detailTitleText != null) detailTitleText.text = "";
        if (detailDescriptionText != null) detailDescriptionText.text = "任務を選択して詳細を確認";
    }
}