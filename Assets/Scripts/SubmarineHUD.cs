using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SubmarineHUD : MonoBehaviour
{
    public static SubmarineHUD Instance;

    [Header("コンソールログ設定")]
    public GameObject logPanel;
    public TMP_Text logText;
    public int maxLines = 6;
    public float logLifetime = 8f;
    private List<LogEntry> logs = new List<LogEntry>();

    [Header("ミッションリスト設定")]
    [Tooltip("アクティブなミッション一覧を表示するテキスト")]
    public TMP_Text missionListText;

    private class LogEntry { public string msg; public float timer; }

    void Awake() { Instance = this; }

    void Start()
    {
        if (logPanel != null) logPanel.SetActive(false);
        if (logText != null) logText.text = "";
    }

    public void AddLog(string message, string hexColor = "#FFFFFF")
    {
        logs.Add(new LogEntry { msg = $"<color={hexColor}>{message}</color>", timer = logLifetime });
        if (logs.Count > maxLines) logs.RemoveAt(0);
        UpdateLogUI();
    }

    // ★追加：GameManagerからミッションリストのテキストを受け取って表示する
    public void UpdateMissionListText(string text)
    {
        if (missionListText != null) missionListText.text = text;
    }

    void Update()
    {
        bool logChanged = false;
        for (int i = logs.Count - 1; i >= 0; i--)
        {
            logs[i].timer -= Time.deltaTime;
            if (logs[i].timer <= 0) { logs.RemoveAt(i); logChanged = true; }
        }
        if (logChanged) UpdateLogUI();
    }

    private void UpdateLogUI()
    {
        if (logText == null) return;
        if (logPanel != null) logPanel.SetActive(logs.Count > 0);

        string fullText = "";
        foreach (var log in logs) fullText += log.msg + "\n";
        logText.text = fullText;
    }
}