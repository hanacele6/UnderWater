using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI設定")]
    public GameObject dialoguePanel; // 会話ウィンドウ全体
    public TextMeshProUGUI nameText; // 名前表示用テキスト
    public TextMeshProUGUI dialogueText; // 本文用テキスト

    public Image portraitImage;

    private Queue<DialogueData.Sentence> sentencesQueue = new Queue<DialogueData.Sentence>();
    private bool isTalking = false;

    private DialogueData currentDialogue; 

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

        currentDialogue = dialogue; 

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
        
        // 名前とテキストの更新
        if (nameText != null) nameText.text = sentence.speakerName;
        if (dialogueText != null) dialogueText.text = sentence.text;

        if (portraitImage != null)
        {
            if (sentence.portrait != null)
            {
                portraitImage.sprite = sentence.portrait;
                portraitImage.gameObject.SetActive(true); // 画像があれば表示
            }
            else
            {
                portraitImage.gameObject.SetActive(false); // なければ隠す
            }
        }
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isTalking = false;
        Debug.Log("会話終了");

        if (currentDialogue != null && !string.IsNullOrEmpty(currentDialogue.flagToSetOnComplete))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetFlag(currentDialogue.flagToSetOnComplete, true);
            }
        }
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