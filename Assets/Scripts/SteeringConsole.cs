using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using TMPro; 

public class SteeringConsole : MonoBehaviour, IInteractable
{
    [Header("連携するオブジェクト")]
    public SubmarineController submarine;
    public PlayerInput playerInput; 
    
    [Tooltip("画面に表示するソナーパネル全体")]
    public GameObject sonarPanel; 

    [Header("カメラ設定")]
    public GameObject fpsCamera;
    public GameObject submarineCamera;
    public GameObject sonarCamera;

    [Header("演出設定")]
    [Tooltip("SteeringConsole専用の暗転パネル（CanvasGroup付き）")]
    public CanvasGroup fadePanel; 
    [Tooltip("暗転中に文字を表示するフルスクリーンのテキストUI")]
    public TMP_Text transitionText;
    [Tooltip("一文字ずつ表示するスピード（秒）")]
    public float typeWriterSpeed = 0.05f;

    [Header("演出設定（効果音）")]
    public AudioClip bootSound;
    public AudioClip airlockSound;
    public AudioClip shutdownSound;
    private AudioSource audioSource;

    [Header("URP設定")]
    public UniversalRenderPipelineAsset urpAsset;

    private bool isPlayerPiloting = false;
    private Coroutine transitionCoroutine; 

    void Start()
    {
        if (sonarPanel != null) sonarPanel.SetActive(false);
        if (transitionText != null) transitionText.text = ""; 
        
        if (fadePanel != null) 
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
        }

        audioSource = GetComponent<AudioSource>(); 
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        SetAllBioAIActive(false); 

        if (fpsCamera != null) fpsCamera.SetActive(true);
        if (submarineCamera != null) submarineCamera.SetActive(false);
        if (sonarCamera != null) sonarCamera.SetActive(false);
    }

    public string GetInteractPrompt()
    {
        return isPlayerPiloting ? "" : "操縦する"; 
    }

    public void Interact()
    {
        if (!isPlayerPiloting && transitionCoroutine == null) StartPiloting();
    }

    void Update()
    {
        if (isPlayerPiloting && Input.GetKeyDown(KeyCode.Q) && transitionCoroutine == null)
        {
            StopPiloting();
        }
    }

    private void StartPiloting()
    {
        isPlayerPiloting = true; 
        if (transitionText != null) transitionText.text = "";
        
        transitionCoroutine = StartCoroutine(StartPilotingSequence());
    }

    private void StopPiloting()
    {
        if (transitionText != null) transitionText.text = "";

        SubmarineStatus subStatus = submarine != null ? submarine.GetComponent<SubmarineStatus>() : null;
        bool hasCargo = subStatus != null && subStatus.cargoQueue.Count > 0;
        
        transitionCoroutine = StartCoroutine(StopPilotingSequence(hasCargo, subStatus));
    }



    private IEnumerator FadeInOut(float targetAlpha, float duration)
    {
        if (fadePanel == null) yield break;
        
        if (!fadePanel.gameObject.activeSelf)
        {
            fadePanel.gameObject.SetActive(true);
        }

        fadePanel.blocksRaycasts = true; // フェード中はクリックなどを防ぐ
        float startAlpha = fadePanel.alpha;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        fadePanel.alpha = targetAlpha;

        // 透明（0）に戻った時の処理
        if (targetAlpha == 0f) 
        {
            fadePanel.blocksRaycasts = false;
            
            fadePanel.gameObject.SetActive(false); 
        }
    }

    private IEnumerator StartPilotingSequence()
    {
        if (playerInput != null) playerInput.enabled = false;
        if (GameManager.Instance != null) GameManager.Instance.LockPlayer(); 
        if (UIManager.Instance != null) UIManager.Instance.SetInteractUIVisible(false);

        yield return StartCoroutine(FadeInOut(1f, 0.4f)); 

        if (fpsCamera != null) fpsCamera.SetActive(false);
        if (submarineCamera != null) submarineCamera.SetActive(true);
        if (sonarCamera != null) sonarCamera.SetActive(true);

        if (audioSource != null && bootSound != null) audioSource.PlayOneShot(bootSound);

        string line1 = ">> ソナーシステム 起動...\n";
        yield return StartCoroutine(TypeWriterEffect(line1));
        yield return new WaitForSeconds(0.5f);

        //string line2 = ">> 全機能 オンライン。\n>> 操縦資格を確認。アライシス・ディープシー。";
        //yield return StartCoroutine(TypeWriterEffect(line2));
        //yield return new WaitForSeconds(1.0f); 

        if (sonarPanel != null) sonarPanel.SetActive(true);
        if (transitionText != null) transitionText.text = ""; 
        
        
        yield return StartCoroutine(FadeInOut(0f, 0.4f)); 

        ExecuteStartPilotingSettings();
        transitionCoroutine = null; 
    }

    private void ExecuteStartPilotingSettings()
    {
        if (playerInput != null) playerInput.enabled = false; 
        if (submarine != null) submarine.isPiloting = true; 

        if (DialogueManager.Instance != null) DialogueManager.Instance.ForceEndDialogue();
        if (UIManager.Instance != null) UIManager.Instance.canOpenMenu = false;

        SetAllBioAIActive(true);
        if (urpAsset != null) urpAsset.renderScale = 1.0f;
    }

    private IEnumerator StopPilotingSequence(bool hasCargo, SubmarineStatus subStatus)
    {
        if (GameManager.Instance != null) GameManager.Instance.LockPlayer(); 

        yield return StartCoroutine(FadeInOut(1f, 0.4f)); 

        if (sonarPanel != null) sonarPanel.SetActive(false);

        if (hasCargo && subStatus != null)
        {
            int count = subStatus.cargoQueue.Count;

            if (audioSource != null && airlockSound != null) audioSource.PlayOneShot(airlockSound);

            string line1 = $">> 警告: 未登録カーゴ{count}個を検知。\n";
            yield return StartCoroutine(TypeWriterEffect(line1));
            yield return new WaitForSeconds(0.4f);

            string line2 = ">> エアロック経由で船内移送を開始...\n";
            yield return StartCoroutine(TypeWriterEffect(line2));
            yield return new WaitForSeconds(1.0f);

            if (CargoPhysicsUI.Instance != null) CargoPhysicsUI.Instance.ClearContainers();

            string line3 = ">> 移送完了。船内にて開封してください。";
            yield return StartCoroutine(TypeWriterEffect(line3));
            yield return new WaitForSeconds(2.0f);
        }
        else
        {
            if (audioSource != null && shutdownSound != null) audioSource.PlayOneShot(shutdownSound);

            string line1 = ">> ソナーシステム シャットダウン...\n";
            yield return StartCoroutine(TypeWriterEffect(line1));
            yield return new WaitForSeconds(0.5f);

            string line2 = ">> 全機能 スタンドバイ。";
            yield return StartCoroutine(TypeWriterEffect(line2));
            yield return new WaitForSeconds(1.5f); 
        }

        if (transitionText != null) transitionText.text = "";
        
        if (fpsCamera != null) fpsCamera.SetActive(true);
        if (submarineCamera != null) submarineCamera.SetActive(false);
        if (sonarCamera != null) sonarCamera.SetActive(false);

        // ★自前のフェードイン
        yield return StartCoroutine(FadeInOut(0f, 0.4f)); 

        ExecuteStopPilotingSettings();
        transitionCoroutine = null; 
    }

    private void ExecuteStopPilotingSettings()
    {
        isPlayerPiloting = false;
        if (playerInput != null) playerInput.enabled = true; 
        if (submarine != null) submarine.isPiloting = false; 

        if (UIManager.Instance != null) 
        {
            UIManager.Instance.SetInteractUIVisible(true);
            UIManager.Instance.canOpenMenu = true;
        }
        
        SetAllBioAIActive(false);

        if (GameManager.Instance != null) GameManager.Instance.UnlockPlayer();
        if (urpAsset != null) urpAsset.renderScale = 0.5f;
    }

    private IEnumerator TypeWriterEffect(string text)
    {
        if (transitionText == null) yield break;

        foreach (char c in text)
        {
            transitionText.text += c;
            yield return new WaitForSeconds(typeWriterSpeed);
        }
    }

    private void SetAllBioAIActive(bool isActive)
    {
        BioAI[] allBios = FindObjectsOfType<BioAI>();
        foreach (BioAI bio in allBios)
        {
            bio.isAIActive = isActive;
        }
    }
}