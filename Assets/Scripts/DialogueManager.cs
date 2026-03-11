using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI;
using UnityEngine.Video; 

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

    // ==========================================
    // 全画面演出（スチル画像・映像）UI
    // ==========================================
    [Header("全画面演出設定")]
    public Image fullScreenImageUI;     // スチル画像を表示するUI
    public RawImage videoDisplayUI;     // 動画を映すUIスクリーン
    public VideoPlayer videoPlayer;     // 動画再生コンポーネント
    
    private bool isWaitingForVideo = false; // 映像が終わるのを待っているか？

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

    private bool isWaitingForChoice = false;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false); 
        if (choicePanel != null) choicePanel.SetActive(false);

        // ★追加：画像と動画の画面を最初は隠しておく
        ClearMedia();

        // ★追加：動画用のスクリーン（RenderTexture）を自動で作成して貼り付ける便利処理
        if (videoPlayer != null && videoDisplayUI != null)
        {
            RenderTexture rt = new RenderTexture(1920, 1080, 16, RenderTextureFormat.ARGB32);
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = rt;
            videoDisplayUI.texture = rt;
        }
    }

    // ==========================================
    // ★追加：画像と動画を画面から消すメソッド
    // ==========================================
    private void ClearMedia()
    {
        if (fullScreenImageUI != null) fullScreenImageUI.gameObject.SetActive(false);
        if (videoDisplayUI != null) videoDisplayUI.gameObject.SetActive(false);
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }
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
        isWaitingForVideo = false; // ★追加
        dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        
        ClearMedia(); // ★追加：強制終了時も画像を消す

        Debug.Log("会話が外部要因で強制終了されました");
    }

    public void StartDialogue(DialogueData dialogue, Action onComplete = null, NPCController npc = null)
    {
        if (isTalking && !isWaitingForChoice && !isWaitingForVideo)
        {
            DisplayNextSentence();
            return;
        }
        
        if (isTalking) ForceEndDialogue(); 
        
        if (npc != null) currentInteractedNPC = npc;
        if (onComplete != null) onCompleteCallback = onComplete;

        dialoguePanel.SetActive(true);
        if (choicePanel != null) choicePanel.SetActive(false); 
        if (UIManager.Instance != null) {
            UIManager.Instance.SetMainMissionPanelVisible(false);
            UIManager.Instance.SetInteractUIVisible(false);
        }
        isTalking = true;
        if (UIManager.Instance != null) UIManager.Instance.SetDialogueMode(true);
        isWaitingForChoice = false;
        isWaitingForVideo = false;
        
        currentSentences = dialogue.sentences;
        currentIndex = 0;
        currentDialogue = dialogue; 

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (isWaitingForChoice || isWaitingForVideo) return; // ★変更：映像待ちの時は次へ進めない

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            string cleanText = System.Text.RegularExpressions.Regex.Replace(currentFullText, "<speed=.*?>", "");
            dialogueText.text = cleanText; 

            CheckForChoicesOrJumps();
            return;
        }

        if (currentSentences == null || currentIndex >= currentSentences.Count)
        {
            EndDialogue();
            return;
        }

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

        // ==========================================
        // ★追加：全画面演出（画像・映像）の反映
        // ==========================================
        if (sentence.clearMedia) ClearMedia(); // メディアを消す指示があれば消す

        if (sentence.fullScreenImage != null && fullScreenImageUI != null)
        {
            fullScreenImageUI.sprite = sentence.fullScreenImage;
            fullScreenImageUI.gameObject.SetActive(true);
        }

        if (sentence.videoClip != null && videoPlayer != null && videoDisplayUI != null)
        {
            videoPlayer.clip = sentence.videoClip;
            videoDisplayUI.gameObject.SetActive(true);
            videoPlayer.Play();

            if (sentence.waitVideoFinish)
            {
                // ★ 映像を待つ設定なら、専用の待機モードに入る
                StartCoroutine(WaitVideoFinishRoutine(sentence));
                return; // ここで処理を止める（文字送りはコルーチン内でやる）
            }
        }

        // 映像待機がない場合は通常通り文字送りを開始
        typingCoroutine = StartCoroutine(TypeSentence(currentFullText));
    }

    // ==========================================
    // ★追加：動画が終わるまで待機する処理
    // ==========================================
    private IEnumerator WaitVideoFinishRoutine(DialogueData.Sentence sentence)
    {
        isWaitingForVideo = true;

        bool hasText = !string.IsNullOrEmpty(sentence.text);

        // 文字が空欄の場合は、会話の黒枠自体を隠して「映画」のようにする
        if (!hasText)
        {
            dialoguePanel.SetActive(false);
        }
        else
        {
            // 文字がある場合は、裏で文字送りを進めておく
            typingCoroutine = StartCoroutine(TypeSentence(sentence.text));
        }

        // 動画の再生が始まるまで少し待つ
        yield return new WaitForSeconds(0.1f);
        yield return new WaitUntil(() => videoPlayer.isPlaying);

        // 動画が終わるまでひたすら待機（クリックしても進みません）
        yield return new WaitWhile(() => videoPlayer.isPlaying);

        isWaitingForVideo = false;

        // 会話の黒枠を元に戻す
        dialoguePanel.SetActive(true);

        // もし「文字空欄（映画モード）」だった場合は、動画が終わったら自動で次のセリフへ進むとスムーズ！
        if (!hasText)
        {
            DisplayNextSentence();
        }
    }

    private void CheckForChoicesOrJumps()
    {
        var currentSentence = currentSentences[currentIndex - 1];

        if (currentSentence.choices != null && currentSentence.choices.Count > 0)
        {
            ShowChoices(currentSentence.choices);
        }
        else if (!string.IsNullOrEmpty(currentSentence.forceJumpLabel))
        {
            if (currentSentence.forceJumpLabel == "END")
            {
                currentIndex = currentSentences.Count;
            }
            else
            {
                JumpToLabel(currentSentence.forceJumpLabel);
            }
        }
    }

    private void ShowChoices(List<DialogueData.Choice> choices)
    {
        isWaitingForChoice = true;
        
        foreach (Transform child in choicePanel.transform) Destroy(child.gameObject);

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

        if (!string.IsNullOrEmpty(selectedChoice.flagToSetOnChoose) && GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag(selectedChoice.flagToSetOnChoose, true);
        }

        JumpToLabel(selectedChoice.jumpToLabel);
    }

    private void JumpToLabel(string targetLabel)
    {
        if (string.IsNullOrEmpty(targetLabel))
        {
            DisplayNextSentence(); 
            return;
        }

        if (targetLabel == "END")
        {
            EndDialogue(); 
            return;
        }

        for (int i = 0; i < currentSentences.Count; i++)
        {
            if (currentSentences[i].label == targetLabel)
            {
                currentIndex = i; 
                DisplayNextSentence();
                return;
            }
        }

        Debug.LogWarning($"ラベル '{targetLabel}' が見つかりませんでした。そのまま次へ進みます。");
        DisplayNextSentence();
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        
        isTalking = false;
        if (UIManager.Instance != null) UIManager.Instance.SetDialogueMode(false);
        isWaitingForChoice = false;
        isWaitingForVideo = false;

        ClearMedia(); // ★追加：会話が終わったら画像と動画を消す

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

        if (UIManager.Instance != null) 
        {
            UIManager.Instance.SetMainMissionPanelVisible(true);
            UIManager.Instance.SetInteractUIVisible(true); 
        }
    }

    void Update()
    {
        // ★変更：isWaitingForVideo（映像待ち）の時もクリックを無効にする
        if (isTalking && !isWaitingForChoice && !isWaitingForVideo && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) 
            {
                return; 
            }
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

        CheckForChoicesOrJumps();
    }
}