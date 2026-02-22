using UnityEngine;
using UnityEngine.InputSystem; 

public class SteeringConsole : MonoBehaviour, IInteractable
{
    [Header("連携するオブジェクト")]
    public SubmarineController submarine;
    public PlayerInput playerInput; 
    
    [Tooltip("画面に表示するソナーパネル全体")]
    public GameObject sonarPanel; 


    private bool isPlayerPiloting = false;

    // ゲーム開始時に必ずソナーを消し、敵の動きも止める
    void Start()
    {
        if (sonarPanel != null) sonarPanel.SetActive(false);
        SetAllBioAIActive(false); 
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

        if (UIManager.Instance.crosshair != null) 
        {
            UIManager.Instance.crosshair.SetActive(false);
        }
        
        if (sonarPanel != null) sonarPanel.SetActive(true);
        UIManager.Instance.canOpenMenu = false;

        // 操縦を始めたら、海中の全生物が動き出す
        SetAllBioAIActive(true);
    }

    private void StopPiloting()
    {
        isPlayerPiloting = false;
        playerInput.enabled = true; 
        submarine.isPiloting = false; 

        if (UIManager.Instance.crosshair != null) 
        {
            UIManager.Instance.crosshair.SetActive(true);
        }
        
        if (sonarPanel != null) sonarPanel.SetActive(false);
        UIManager.Instance.canOpenMenu = true;

        // 操縦をやめたら、海中の全生物の時間が止まる
        SetAllBioAIActive(false);
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