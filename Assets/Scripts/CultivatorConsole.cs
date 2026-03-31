using UnityEngine;
using System.Collections.Generic;

public class CultivatorConsole : MonoBehaviour, IInteractable
{
    [Header("UI・カメラ連携")]
    public GameObject cultivatorPanel; // 培養用のUIパネル
    // public CultivatorUI cultivatorUI; // ※後で作ります
    public GameObject playerCamera;
    public GameObject deskCamera; 
    
    [Header("カメラ移動設定")]
    public Vector3 cultivatorCamLocalPos; 
    public Vector3 cultivatorCamLocalRot; 

    [Header("花壇の土（スロット）一覧")]
    public List<PlanterSlot> planterSlots = new List<PlanterSlot>();

    public string GetInteractPrompt()
    {
        return "培養ポッドを使う";
    }

    public void Interact()
    {
        InteractableHighlight highlight = GetComponent<InteractableHighlight>();
        if (highlight != null)
        {
            highlight.ChangeHighlightState(InteractableHighlight.HighlightState.None);
            highlight.isHighlightable = false; 
        }

        // カメラを花壇の特等席へ移動
        if (deskCamera != null) 
        {
            deskCamera.transform.localPosition = cultivatorCamLocalPos;
            deskCamera.transform.localRotation = Quaternion.Euler(cultivatorCamLocalRot); 
        }

        if (cultivatorPanel != null) cultivatorPanel.SetActive(true);
        
        // 💡 ここで CultivatorUI を Open します（今回は省略）

        if (GameManager.Instance != null) GameManager.Instance.LockPlayer();
        if (UIManager.Instance != null) {
            UIManager.Instance.SetDialogueMode(true); 
            UIManager.Instance.SetHUDVisible(false); 
        }

        if (playerCamera != null) playerCamera.SetActive(false);
        if (deskCamera != null) deskCamera.SetActive(true);
    }

    // UIを閉じる時の処理（UI側の Close ボタン等から呼ばれる想定）
    public void CloseConsole()
    {
        if (cultivatorPanel != null) cultivatorPanel.SetActive(false);

        if (playerCamera != null) playerCamera.SetActive(true);
        if (deskCamera != null) deskCamera.SetActive(false);

        InteractableHighlight highlight = GetComponent<InteractableHighlight>();
        if (highlight != null) highlight.isHighlightable = true;

        if (GameManager.Instance != null) GameManager.Instance.UnlockPlayer();
        if (UIManager.Instance != null) {
            UIManager.Instance.SetDialogueMode(false); 
            UIManager.Instance.SetHUDVisible(true); 
        }
    }
}