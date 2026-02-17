using UnityEngine;
using UnityEngine.InputSystem; 

public class SteeringConsole : MonoBehaviour, IInteractable
{
    [Header("連携するオブジェクト")]
    public SubmarineController submarine;
    public PlayerInput playerInput; 
    
    // ★追加：表示・非表示を切り替えるソナー画面の大元
    [Tooltip("画面に表示するソナーパネル全体")]
    public GameObject sonarPanel; 

    private bool isPlayerPiloting = false;

    public string GetInteractPrompt()
    {
        return isPlayerPiloting ? "" : "操縦する"; 
    }

    public void Interact()
    {
        if (!isPlayerPiloting)
        {
            StartPiloting();
        }
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
        
        // ★変更：メッセージの代わりに、ソナーパネルを表示（ON）にする
        if (sonarPanel != null)
        {
            sonarPanel.SetActive(true);
        }
    }

    private void StopPiloting()
    {
        isPlayerPiloting = false;
        playerInput.enabled = true; 
        submarine.isPiloting = false; 
        
        // ★変更：メッセージを消す代わりに、ソナーパネルを非表示（OFF）にする
        if (sonarPanel != null)
        {
            sonarPanel.SetActive(false);
        }
    }
}