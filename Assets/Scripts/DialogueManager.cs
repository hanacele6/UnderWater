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
    public bool isTalking = false;

    private DialogueData currentDialogue; 

    [Header("オーディオ設定")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typeSound;
    
    private float currentPitch = 1.0f;

    private Coroutine typingCoroutine;

    private string currentFullText;

    private NPCController currentInteractedNPC;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false); // 最初は隠しておく
    }

    public void ForceEndDialogue()
    {
        // 実行中のタイピング演出を止める
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        sentencesQueue.Clear();
        currentDialogue = null;
        isTalking = false;
        dialoguePanel.SetActive(false);
        
        Debug.Log("会話が外部要因で強制終了されました");
    }

    // 会話を開始する（外部から呼ばれる）
    public void StartDialogue(DialogueData dialogue, NPCController npc = null)
    {
        if (isTalking)
        {
            DisplayNextSentence();
            return;
        }
        if (isTalking) ForceEndDialogue();
        if (npc != null) currentInteractedNPC = npc;

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
        // 1. タイピング中にクリックされたら、コルーチンを止めて全文を一気に表示
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            string cleanText = System.Text.RegularExpressions.Regex.Replace(currentFullText, "<speed=.*?>", "");
            dialogueText.text = cleanText; 
            return;
        }

        

        // 2. キューが空なら終了
        if (sentencesQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        // 3. 次のセリフを取り出す
        var sentence = sentencesQueue.Dequeue();
        currentFullText = sentence.text; // 全文を保存しておく

        // 4. UIの更新
        if (nameText != null) nameText.text = sentence.speakerName;
        
        if (portraitImage != null)
        {
            if (sentence.portrait != null)
            {
                portraitImage.sprite = sentence.portrait;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        // 5. タイピング演出開始
        currentFullText = sentence.text;
        currentPitch = sentence.voicePitch; // ピッチを取得（0なら1にする等のガードを入れると安全）
        if (currentPitch <= 0) currentPitch = 1.0f; 

        typingCoroutine = StartCoroutine(TypeSentence(currentFullText));
    }

    

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isTalking = false;
        Debug.Log("会話終了");
        if (currentInteractedNPC != null)
        {
            currentInteractedNPC.StartReturningRotation();
            currentInteractedNPC = null;
        }

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

    IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = "";
        float currentSpeed = 0.05f;

        for (int i = 0; i < sentence.Length; i++)
        {
            if (sentence[i] == '<')
            {
                int closingBracket = sentence.IndexOf('>', i);
                if (closingBracket != -1)
                {
                    string tag = sentence.Substring(i, closingBracket - i + 1);
                    
                    if (tag.StartsWith("<speed="))
                    {
                        string value = tag.Replace("<speed=", "").Replace(">", "");
                        float.TryParse(value, out currentSpeed);
                        i = closingBracket;
                        continue;
                    }
                    else
                    {
                        // TMP標準タグ（<color>等）の場合、タグを一気にテキストに追加
                        dialogueText.text += tag;
                        i = closingBracket;
                        continue;
                    }
                }
            }

            dialogueText.text += sentence[i];

            if (audioSource != null && typeSound != null && !char.IsWhiteSpace(sentence[i]))
            {
                audioSource.pitch = currentPitch;
                audioSource.PlayOneShot(typeSound);
            }

            yield return new WaitForSeconds(currentSpeed);
        }
        typingCoroutine = null; // 終わったらnullに戻す
    }
}