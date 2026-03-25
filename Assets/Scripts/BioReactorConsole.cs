using UnityEngine;

public class BioReactorConsole : MonoBehaviour, IInteractable
{
    [Header("UI連携")]
    public GameObject bioReactorPanel; 
    public BioReactorUI reactorUI;

    [Header("カメラ連携")]
    public GameObject playerCamera; 
    public GameObject deskCamera; 

    public string GetInteractPrompt()
    {
        return "バイオリアクターを使う";
    }

    public void Interact()
    {

        InteractableHighlight highlight = GetComponent<InteractableHighlight>();
        if (highlight != null)
        {
            // 強制的に光を消し、実験中は「光らない状態」にロックする
            highlight.ChangeHighlightState(InteractableHighlight.HighlightState.None);
            highlight.isHighlightable = false; 
        }

        // 2. UIを表示し、UI側に「閉じたらカメラ等を戻してね」と約束（コールバック）を渡す
        if (bioReactorPanel != null) bioReactorPanel.SetActive(true);
        if (reactorUI != null)
        {
            reactorUI.OpenReactorUI(() => 
            {
                // カメラをプレイヤーに戻す
                if (playerCamera != null) playerCamera.SetActive(true);
                if (deskCamera != null) deskCamera.SetActive(false);

                // 机のハイライト許可を元に戻す（また光るようになる）
                if (highlight != null) highlight.isHighlightable = true;
            });
        }


        if (GameManager.Instance != null) GameManager.Instance.LockPlayer();
        
        // カーソルを出す（ここで一瞬メニューボタンが出ちゃう）
        if (UIManager.Instance != null) UIManager.Instance.SetDialogueMode(true); 
        
        // メニューごとHUDを完全粉砕してスッキリさせる！
        if (UIManager.Instance != null) UIManager.Instance.SetHUDVisible(false); 

        // 4. カメラの切り替え（実験机のカメラへ！）
        if (playerCamera != null) playerCamera.SetActive(false);
        if (deskCamera != null) deskCamera.SetActive(true);
    }
}