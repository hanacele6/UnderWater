using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshProを使う場合

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI設定")]
    public GameObject dialoguePanel; // 会話ウィンドウ全体
    public TextMeshProUGUI nameText; // 名前表示用テキスト
    public TextMeshProUGUI dialogueText; // 本文用テキスト

    private Queue<DialogueData.Sentence> sentencesQueue = new Queue<DialogueData.Sentence>();
    private bool isTalking = false;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false); // 最初は隠しておく
    }

    // 会話を開始する（外部から呼ばれる）
    public void StartDialogue(DialogueData dialogue)
    {
        dialoguePanel.SetActive(true);
        isTalking = true;
        sentencesQueue.Clear();

        // 会話データをキュー（待ち行列）に入れる
        foreach (var sentence in dialogue.sentences)
        {
            sentencesQueue.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    // 次のセリフを表示する
    public void DisplayNextSentence()
    {
        if (sentencesQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        var sentence = sentencesQueue.Dequeue();
        nameText.text = sentence.speakerName;
        dialogueText.text = sentence.text;
        
        // ★ここを「1文字ずつ表示」にする演出を入れると更に雰囲気が出ます
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isTalking = false;
        Debug.Log("会話終了");
    }

    void Update()
    {
        // 会話中にクリックorスペースキーで次へ
        if (isTalking && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            DisplayNextSentence();
        }
    }
}