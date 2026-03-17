using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class SteeringConsole : MonoBehaviour, IInteractable
{
    [Header("連携するオブジェクト")]
    public SubmarineController submarine;
    public PlayerInput playerInput; 
    
    [Tooltip("画面に表示するソナーパネル全体")]
    public GameObject sonarPanel; 

    // --- カメラの参照 ---
    [Header("カメラ設定")]
    [Tooltip("普段のプレイヤー視点カメラ")]
    public GameObject fpsCamera;
    [Tooltip("潜水艦のモニター用カメラ")]
    public GameObject submarineCamera;
    [Tooltip("裏方のソナー撮影用カメラ")]
    public GameObject sonarCamera;
    // ------------------------

    [Header("URP設定")]
    [Tooltip("現在使用しているURP Assetを入れてください")]
    public UniversalRenderPipelineAsset urpAsset;

    private bool isPlayerPiloting = false;

    // ゲーム開始時に必ずソナーを消し、敵の動きも止める
    void Start()
    {
        if (sonarPanel != null) sonarPanel.SetActive(false);
        SetAllBioAIActive(false); 

        // --- 追加：ゲーム開始時はFPSカメラだけにする ---
        if (fpsCamera != null) fpsCamera.SetActive(true);
        if (submarineCamera != null) submarineCamera.SetActive(false);
        if (sonarCamera != null) sonarCamera.SetActive(false);
        // ---------------------------------------------
    }

    public string GetInteractPrompt()
    {
        return isPlayerPiloting ? "" : "操縦する"; 
    }

    public void Interact()
    {
        if (!isPlayerPiloting) StartPiloting();
    }

    void Update()
    {
        if (isPlayerPiloting && Input.GetKeyDown(KeyCode.Q))
        {
            StopPiloting();
        }
    }

    private void StartPiloting()
    {
        isPlayerPiloting = true;
        playerInput.enabled = false; 
        submarine.isPiloting = true; 

        if (UIManager.Instance != null) 
        {
            UIManager.Instance.SetInteractUIVisible(false);
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ForceEndDialogue();
        }
        
        if (sonarPanel != null) sonarPanel.SetActive(true);
        UIManager.Instance.canOpenMenu = false;

        SetAllBioAIActive(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LockPlayer();
        }

        // --- カメラ切り替え ---
        if (fpsCamera != null) fpsCamera.SetActive(false);
        if (submarineCamera != null) submarineCamera.SetActive(true);
        if (sonarCamera != null) sonarCamera.SetActive(true);

        if (urpAsset != null)
        {
            urpAsset.renderScale = 1.0f;
        }
    }

    private void StopPiloting()
    {
        isPlayerPiloting = false;
        playerInput.enabled = true; 
        submarine.isPiloting = false; 

        if (UIManager.Instance != null) 
        {
            UIManager.Instance.SetInteractUIVisible(true);
        }
        
        if (sonarPanel != null) sonarPanel.SetActive(false);
        UIManager.Instance.canOpenMenu = true;

        SetAllBioAIActive(false);


        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnlockPlayer();
        }

        // --- カメラ切り替え ---
        if (fpsCamera != null) fpsCamera.SetActive(true);
        if (submarineCamera != null) submarineCamera.SetActive(false);
        if (sonarCamera != null) sonarCamera.SetActive(false);

        if (urpAsset != null)
        {
            urpAsset.renderScale = 0.5f;
        }
    }

    // シーン内のすべてのBioAIを一括でON/OFFする便利メソッド
    private void SetAllBioAIActive(bool isActive)
    {
        BioAI[] allBios = FindObjectsOfType<BioAI>();
        foreach (BioAI bio in allBios)
        {
            bio.isAIActive = isActive;
        }
    }
}