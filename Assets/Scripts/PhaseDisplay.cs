using UnityEngine;
using TMPro;

public class PhaseDisplay : MonoBehaviour
{
    [Header("ニキシー管システム")]
    public GameObject nixieTubePrefab; // Step1で作ったプレハブを入れる
    public Transform tubeContainer;    // Step2で作ったNixieContainerを入れる

    [Header("フェーズ名")]
    public string textBriefing = "BRIEFING";
    public string textOperation = "OPERATION";
    public string textEventCheck = "CHECKING";
    public string textIncident = "Event";
    public string textFreeTime = "STANDBY";

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged.AddListener(UpdateText);
            UpdateText(GameManager.Instance.currentPhase);
        }
    }

    void Update()
    {
        if (UIManager.Instance != null && tubeContainer != null)
        {
            // メニューが開いていない時だけ表示（開いていたら隠す）
            bool shouldShow = !UIManager.Instance.isMenuOpen;
            
            // 状態が違う時だけ SetActive を切り替える（毎フレーム実行するのを防ぐため）
            if (tubeContainer.gameObject.activeSelf != shouldShow)
            {
                tubeContainer.gameObject.SetActive(shouldShow);
            }
        }
    }

    void UpdateText(GamePhase phase)
    {
        string displayText = "";

        switch (phase)
        {
            case GamePhase.Briefing: displayText = textBriefing; break;
            case GamePhase.Operation: displayText = textOperation; break;
            case GamePhase.EventCheck: displayText = textEventCheck; break;
            case GamePhase.Incident: displayText = textIncident; break;
            case GamePhase.FreeTime: displayText = textFreeTime; break;
        }

        // 1. 今表示されている古い管をすべて破壊（リセット）する
        foreach (Transform child in tubeContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. 新しい文字の長さだけ、プレハブを生成する
        foreach (char c in displayText)
        {
            // スペース（空白）の場合は何も書かない管を作るか、隙間を空けます
            if (c == ' ') continue;

            // プレハブをコンテナの中に生成
            GameObject newTube = Instantiate(nixieTubePrefab, tubeContainer);
            
            // 生成した管の中にある TextMeshPro を探して、1文字だけセットする
            TextMeshProUGUI txt = newTube.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = c.ToString();
            }
        }
    }
}