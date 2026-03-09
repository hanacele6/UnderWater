using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI設定")]
    public GameObject dialoguePanel; 
    public TextMeshProUGUI nameText; 
    public TextMeshProUGUI dialogueText; 
    public Image portraitImage;

    [Header("選択肢UI設定")]
    public GameObject choicePanel;         // 選択肢ボタンを並べる親パネル
    public GameObject choiceButtonPrefab;  // 選択肢ボタンのプレハブ

    // ★ 変更：Queue（ところてん方式）をやめ、リストと行数（インデックス）で管理する
    private List<DialogueData.Sentence> currentSentences;
    private int currentIndex = 0;

    public bool isTalking = false;
    private DialogueData currentDialogue; 

    [Header("オーディオ設定")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typeSound;
    private float currentPitch = 1.0f;
    private Coroutine typingCoroutine;
    private string currentFullText;
    private NPCController currentInteractedNPC;
    private Action onCompleteCallback;

    // 選択肢待ち状態かどうか
    private bool isWaitingForChoice = false;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false); 
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    public void ForceEndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        currentSentences = null;
        currentDialogue = null;
        isTalking = false;
        isWaitingForChoice = false;
        dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        
        Debug.Log("会話が外部要因で強制終了されました");
    }

    public void StartDialogue(DialogueData dialogue, Action onComplete = null, NPCController npc = null)
    {
        if (isTalking && !isWaitingForChoice)
        {
            DisplayNextSentence();
            return;
        }
        
        if (isTalking) ForceEndDialogue(); 
        
        if (npc != null) currentInteractedNPC = npc;
        if (onComplete != null) onCompleteCallback = onComplete;

        dialoguePanel.SetActive(true);
        if (choicePanel != null) choicePanel.SetActive(false); 
        if (UIManager.Instance != null) UIManager.Instance.SetMainMissionPanelVisible(false);
        
        isTalking = true;
        isWaitingForChoice = false;
        
        // ★ 変更：リストをセットして、1行目（0）からスタートする
        currentSentences = dialogue.sentences;
        currentIndex = 0;
        currentDialogue = dialogue; 

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (isWaitingForChoice) return;

        if (typingCoroutine != null)
        {
            // タイピング中なら一気に全部表示するスキップ処理
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            string cleanText = System.Text.RegularExpressions.Regex.Replace(currentFullText, "<speed=.*?>", "");
            dialogueText.text = cleanText; 

            // ★ タイピングをスキップした直後に、もし選択肢やジャンプ指定があれば即発動させる
            CheckForChoicesOrJumps();
            return;
        }

        // ★ 変更：行が最後まで到達したら終了
        if (currentSentences == null || currentIndex >= currentSentences.Count)
        {
            EndDialogue();
            return;
        }

        // ★ 変更：次のセリフを取り出して、行数を1つ進める
        var sentence = currentSentences[currentIndex];
        currentIndex++; 
        
        currentFullText = sentence.text; 

        if (nameText != null) nameText.text = sentence.speakerName;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCameraTarget(sentence.speakerName);
        }

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

        currentPitch = sentence.voicePitch; 
        if (currentPitch <= 0) currentPitch = 1.0f; 

        typingCoroutine = StartCoroutine(TypeSentence(currentFullText));
    }

    // ★ 新規追加：セリフ表示後に選択肢やジャンプがないか確認する処理
    private void CheckForChoicesOrJumps()
    {
        // 読み終わったばかりの行（currentIndexはすでに進んでいるので -1 する）
        var currentSentence = currentSentences[currentIndex - 1];

        // ① 選択肢があれば表示する
        if (currentSentence.choices != null && currentSentence.choices.Count > 0)
        {
            ShowChoices(currentSentence.choices);
        }
        // ② 強制ジャンプ（またはEND）の指示があれば飛ぶ
        else if (!string.IsNullOrEmpty(currentSentence.forceJumpLabel))
        {
            JumpToLabel(currentSentence.forceJumpLabel);
        }
    }

    // ==========================================
    // 選択肢とジャンプの処理
    // ==========================================
    private void ShowChoices(List<DialogueData.Choice> choices)
    {
        isWaitingForChoice = true;
        
        foreach (Transform child in choicePanel.transform)
        {
            Destroy(child.gameObject);
        }

        choicePanel.SetActive(true);

        foreach (var choice in choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choicePanel.transform);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = choice.choiceText;

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnChoiceSelected(choice));
            }
        }
    }

    private void OnChoiceSelected(DialogueData.Choice selectedChoice)
    {
        choicePanel.SetActive(false);
        isWaitingForChoice = false;

        // フラグを立てる
        if (!string.IsNullOrEmpty(selectedChoice.flagToSetOnChoose) && GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag(selectedChoice.flagToSetOnChoose, true);
        }

        // ★ 指定されたラベルへジャンプする！
        JumpToLabel(selectedChoice.jumpToLabel);
    }

    // ★ 新規追加：ラベルを探して行数をワープする機能
    private void JumpToLabel(string targetLabel)
    {
        if (string.IsNullOrEmpty(targetLabel))
        {
            DisplayNextSentence(); // ラベルが無ければそのまま次の行へ
            return;
        }

        if (targetLabel == "END")
        {
            EndDialogue(); // ENDという指示なら即終了
            return;
        }

        // リストの中からラベルが一致する行を探す
        for (int i = 0; i < currentSentences.Count; i++)
        {
            if (currentSentences[i].label == targetLabel)
            {
                currentIndex = i; // 行数（インデックス）を上書きしてワープ！
                DisplayNextSentence();
                return;
            }
        }

        Debug.LogWarning($"ラベル '{targetLabel}' が見つかりませんでした。そのまま次へ進みます。");
        DisplayNextSentence();
    }

    // ==========================================
    // 終了処理とコルーチン
    // ==========================================
    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        
        isTalking = false;
        isWaitingForChoice = false;
        
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

        onCompleteCallback?.Invoke();
        onCompleteCallback = null;

        if (UIManager.Instance != null) UIManager.Instance.SetMainMissionPanelVisible(true);
    }

    void Update()
    {
        if (isTalking && !isWaitingForChoice && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
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
        typingCoroutine = null; 

        // ★ 追加：文字送りが終わった直後に、選択肢やジャンプがないか確認する
        CheckForChoicesOrJumps();
    }
}