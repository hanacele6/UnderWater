using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BioReactorConsole : MonoBehaviour, IInteractable
{
    public enum ReactorState { Mixing, MovingToCentrifuge, Centrifuging, Results }
    public ReactorState currentState = ReactorState.Mixing;

    [Header("UI連携")]
    public GameObject bioReactorPanel; 
    public BioReactorUI reactorUI;

    [Header("カメラ連携")]
    public GameObject playerCamera; 
    public GameObject deskCamera; 
    
   [Header("カメラ移動設定")]
    public Vector3 mixingCamLocalPos; 
    public Vector3 mixingCamLocalRot; 
    public Vector3 centrifugeCamLocalPos; 
    public Vector3 centrifugeCamLocalRot; 
    public float transitionDuration = 1.5f;

    [Header("ギミック連携")]
    public Centrifuge centrifuge;
    public Transform pipetteContainer; 
    public Transform centrifugeSlots;  

    private bool extractionButtonShown = false;

    void Start()
    {

        if (deskCamera != null)
        {
            deskCamera.transform.localPosition = mixingCamLocalPos;
            deskCamera.transform.localRotation = Quaternion.Euler(mixingCamLocalRot); 
        }
    }

    void Update()
    {

        if (reactorUI == null) return;

        bool isFull = AllPipettesFull();
        bool isCentrifuging = (currentState != ReactorState.Mixing);

        // 液体量、ピペット満タンフラグ、現在移動中かどうかをUIに伝える
        reactorUI.UpdateUIState(FlaskReceiver.Instance.currentLiquidAmount, isFull, isCentrifuging);

        if (currentState == ReactorState.Mixing && !extractionButtonShown)
        {
            if (AllPipettesFull())
            {
                reactorUI.SetExtractionButtonVisible(true);
                extractionButtonShown = true; // 何度も呼ばないためのロック
            }
        }
    }

    private bool AllPipettesFull()
    {
        if (pipetteContainer == null) return false;

        PipetteReceiver[] deskPipettes = pipetteContainer.GetComponentsInChildren<PipetteReceiver>();
        
        if (deskPipettes.Length == 0) return false;

        foreach (var p in deskPipettes)
        {
            if (!p.IsFull || p.receivedPotion == null) return false;
        }
        
        return true;
    }

    public string GetInteractPrompt() => "バイオリアクターを使う";

    public void Interact()
    {
        InteractableHighlight highlight = GetComponent<InteractableHighlight>();
        if (highlight != null)
        {
            highlight.ChangeHighlightState(InteractableHighlight.HighlightState.None);
            highlight.isHighlightable = false; 
        }

        currentState = ReactorState.Mixing;

        if (pipetteContainer != null) pipetteContainer.gameObject.SetActive(true);
        if (centrifugeSlots != null) centrifugeSlots.gameObject.SetActive(false);
        
        foreach(var p in pipetteContainer.GetComponentsInChildren<PipetteReceiver>()) p.EmptyPipette();
        foreach(var p in centrifugeSlots.GetComponentsInChildren<PipetteReceiver>()) p.EmptyPipette();

        extractionButtonShown = false;
        if (deskCamera != null) {
            deskCamera.transform.localPosition = mixingCamLocalPos;
            deskCamera.transform.localRotation = Quaternion.Euler(mixingCamLocalRot);
        }
        if (bioReactorPanel != null) bioReactorPanel.SetActive(true);
        if (reactorUI != null)
        {
            reactorUI.OpenReactorUI(() => 
            {
                if (playerCamera != null) playerCamera.SetActive(true);
                if (deskCamera != null) deskCamera.SetActive(false);
                if (highlight != null) highlight.isHighlightable = true;
                
                if (GameManager.Instance != null) 
                {
                    GameManager.Instance.isUIOpen = false;
                    GameManager.Instance.UnlockPlayer();
                }
                
                if (UIManager.Instance != null) 
                {
                    UIManager.Instance.SetInteractUIVisible(true); 
                    UIManager.Instance.SetDialogueMode(false);
                    UIManager.Instance.SetHUDVisible(true);
                }
            });
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isUIOpen = true;
            GameManager.Instance.LockPlayer();
        }

        if (UIManager.Instance != null) {
            UIManager.Instance.SetInteractUIVisible(false);
            UIManager.Instance.SetDialogueMode(true); 
            UIManager.Instance.SetHUDVisible(false); 
        }

        if (playerCamera != null) playerCamera.SetActive(false);
        if (deskCamera != null) deskCamera.SetActive(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerInteractEvent("BioReactor");
        }
    }

    // 💡 抽出ボタンが押されたら呼ばれる
    public void StartExtractionTransition()
    {
        if (currentState != ReactorState.Mixing) return;
        StartCoroutine(TransitionSequence());
    }

    

    private IEnumerator TransitionSequence()
    {
        currentState = ReactorState.MovingToCentrifuge;
        reactorUI.SetExtractionButtonVisible(false);

        MovePipettesToCentrifuge();

        if (deskCamera != null)
        {
            float elapsed = 0;
            Vector3 startPos = deskCamera.transform.localPosition;
            Quaternion startRot = deskCamera.transform.localRotation; 
            Quaternion targetRot = Quaternion.Euler(centrifugeCamLocalRot);

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / transitionDuration);
                
                // 位置の移動
                deskCamera.transform.localPosition = Vector3.Lerp(startPos, centrifugeCamLocalPos, t);
                deskCamera.transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                
                yield return null;
            }
            // ズレ防止のため、最後に目標値をピタッと入れる
            deskCamera.transform.localPosition = centrifugeCamLocalPos;
            deskCamera.transform.localRotation = targetRot;
        }

        currentState = ReactorState.Centrifuging;
        reactorUI.SetStartSpinButtonVisible(true);
    }

    private void MovePipettesToCentrifuge()
    {
        if (pipetteContainer != null && centrifugeSlots != null)
        {
            // 💡 データのコピー処理を追加
            PipetteReceiver[] deskPipettes = pipetteContainer.GetComponentsInChildren<PipetteReceiver>();
            PipetteReceiver[] centrifugePipettes = centrifugeSlots.GetComponentsInChildren<PipetteReceiver>();

            // 本数が一致している前提で、データを転送する
            for (int i = 0; i < deskPipettes.Length && i < centrifugePipettes.Length; i++)
            {
                centrifugePipettes[i].ReceiveLiquid(deskPipettes[i].currentLiquid, deskPipettes[i].receivedPotion);
            }

            // 見た目の切り替え
            pipetteContainer.gameObject.SetActive(false);
            centrifugeSlots.gameObject.SetActive(true);
        }
    }

    // 遠心分離完了時
    public void ShowResults(List<ItemData> finalPotions)
    {
        currentState = ReactorState.Results;
        reactorUI.SetStartSpinButtonVisible(false);
        
        if (ExtractionUIManager.Instance != null)
        {
            ExtractionUIManager.Instance.OpenExtractionUI(finalPotions, () => {
                ResetConsole();
            });
        }
    }

    private void ResetConsole()
    {
        currentState = ReactorState.Mixing;
        extractionButtonShown = false;
        
        if (pipetteContainer != null) pipetteContainer.gameObject.SetActive(true);
        if (centrifugeSlots != null) centrifugeSlots.gameObject.SetActive(false);
        if (deskCamera != null) deskCamera.transform.localPosition = mixingCamLocalPos;
        
        if (reactorUI != null) reactorUI.CloseUI();
    }
}