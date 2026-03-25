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

    [Header("UI設定（ソナー通信時）")]
    public GameObject sonarDialoguePanel; 
    public TextMeshProUGUI sonarNameText; 
    public TextMeshProUGUI sonarDialogueText; 
    public Image sonarPortraitImage;
    
    [HideInInspector]
    public bool isRadioMode = false;

    [Header("選択肢UI設定")]
    public GameObject choicePanel;         // 選択肢ボタンを並べる親パネル
    public GameObject choiceButtonPrefab;  // 選択肢ボタンのプレハブ

    [Header("自動送り設定")]
    [Tooltip("ONにすると、通信中（ラジオモード）は自動で会話が進みます")]
    public bool autoAdvanceInRadio = true; 
    [Tooltip("文字が表示し終わってから、次に進むまでの待機時間（秒）")]
    public float autoAdvanceTime = 3.0f;   

    private Coroutine autoAdvanceCoroutine; // 自動送りのタイマー

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

        ClearMedia();

        if (videoPlayer != null && videoDisplayUI != null)
        {
            RenderTexture rt = new RenderTexture(1920, 1080, 16, RenderTextureFormat.ARGB32);
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = rt;
            videoDisplayUI.texture = rt;
        }
    }

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
        isWaitingForVideo = false; 
        dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        
        ClearMedia(); 

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


        if (isRadioMode)
        {
            if (sonarDialoguePanel != null) sonarDialoguePanel.SetActive(true);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (sonarDialoguePanel != null) sonarDialoguePanel.SetActive(false);
        }

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
        if (isWaitingForChoice || isWaitingForVideo) return; 

        // 自動送りのタイマーが動いていたら止める
        if (autoAdvanceCoroutine != null) 
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        TextMeshProUGUI activeDialogueText = isRadioMode ? sonarDialogueText : dialogueText;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            string cleanText = System.Text.RegularExpressions.Regex.Replace(currentFullText, "<speed=.*?>", "");
            if (activeDialogueText != null) activeDialogueText.text = cleanText; 

            CheckForChoicesOrJumps();

            var currentSentenceRef = currentSentences[currentIndex - 1];
            if (autoAdvanceInRadio && isRadioMode && (currentSentenceRef.choices == null || currentSentenceRef.choices.Count == 0))
            {
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine());
            }
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

        TextMeshProUGUI activeNameText = isRadioMode ? sonarNameText : nameText;
        Image activePortrait = isRadioMode ? sonarPortraitImage : portraitImage;

        if (activeNameText != null) activeNameText.text = sentence.speakerName;
        
        if (GameManager.Instance != null) GameManager.Instance.UpdateCameraTarget(sentence.speakerName);

        if (activePortrait != null)
        {
            if (sentence.portrait != null)
            {
                activePortrait.sprite = sentence.portrait;
                activePortrait.gameObject.SetActive(true);
            }
            else activePortrait.gameObject.SetActive(false);
        }

        currentPitch = sentence.voicePitch; 
        if (currentPitch <= 0) currentPitch = 1.0f; 

        if (sentence.clearMedia) ClearMedia(); 

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
                StartCoroutine(WaitVideoFinishRoutine(sentence, activeDialogueText));
                return; 
            }
        }


        typingCoroutine = StartCoroutine(TypeSentence(currentFullText, activeDialogueText)); 
    }

    private IEnumerator WaitVideoFinishRoutine(DialogueData.Sentence sentence, TextMeshProUGUI targetTextUI)
    {
        isWaitingForVideo = true;

        bool hasText = !string.IsNullOrEmpty(sentence.text);

        GameObject activePanel = isRadioMode ? sonarDialoguePanel : dialoguePanel;

        if (!hasText)
        {
            if (activePanel != null) activePanel.SetActive(false);
        }
        else
        {
            typingCoroutine = StartCoroutine(TypeSentence(sentence.text, targetTextUI));
        }

        yield return new WaitForSeconds(0.1f);
        yield return new WaitUntil(() => videoPlayer.isPlaying);
        yield return new WaitWhile(() => videoPlayer.isPlaying);

        isWaitingForVideo = false;

        if (activePanel != null) activePanel.SetActive(true);

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
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (sonarDialoguePanel != null) sonarDialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        
        isTalking = false;
        if (UIManager.Instance != null) UIManager.Instance.SetDialogueMode(false);
        isWaitingForChoice = false;
        isWaitingForVideo = false;

        ClearMedia(); 

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
            if (!isRadioMode)
            {
                UIManager.Instance.SetInteractUIVisible(true); 
            }
        }
    }

    void Update()
    {
        if (isTalking && !isWaitingForChoice && !isWaitingForVideo)
        {
            bool tryAdvance = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

            if (tryAdvance)
            {
                if (Input.GetMouseButtonDown(0) && IsPointerOverUIButton()) 
                {
                    return; 
                }
                
                DisplayNextSentence();
            }
        }
    }
    

    private IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSeconds(autoAdvanceTime);
        DisplayNextSentence();
    }


    IEnumerator TypeSentence(string sentence, TextMeshProUGUI targetTextUI)
    {
        if (targetTextUI != null) targetTextUI.text = "";
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
                        if (targetTextUI != null) targetTextUI.text += tag;
                        i = closingBracket;
                        continue;
                    }
                }
            }

            if (targetTextUI != null) targetTextUI.text += sentence[i];

            if (audioSource != null && typeSound != null && !char.IsWhiteSpace(sentence[i]))
            {
                audioSource.pitch = currentPitch;
                audioSource.PlayOneShot(typeSound);
            }

            yield return new WaitForSeconds(currentSpeed);
        }
        typingCoroutine = null; 

        CheckForChoicesOrJumps();


        var currentSentence = currentSentences[currentIndex - 1];
        if (autoAdvanceInRadio && isRadioMode && (currentSentence.choices == null || currentSentence.choices.Count == 0))
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine());
        }
    }

    private bool IsPointerOverUIButton()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;
        
        UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        eventData.position = Input.mousePosition;
        List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);
        
        foreach (UnityEngine.EventSystems.RaycastResult result in results)
        {
            // Buttonコンポーネントか、Toggleコンポーネントが付いているUIだけを「ボタン」とみなす！
            if (result.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null || 
                result.gameObject.GetComponentInParent<UnityEngine.UI.Toggle>() != null)
            
            {
                return true; 
            }
        }
        return false; // ただの背景画像なら false（クリックで会話が進む）
    }
}