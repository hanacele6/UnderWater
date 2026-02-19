using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;

    // カメラのコントローラー変数はもう不要なので削除しました！
    private InteractableHighlight currentHighlightTarget;

    void Update()
    {
        Camera mainCamera = Camera.main;
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // ==========================================
            // ① アウトラインの判定
            // ==========================================
            InteractableHighlight highlightMark = hit.collider.GetComponentInParent<InteractableHighlight>();

            // 見ている対象が変わった時だけ処理する
            if (currentHighlightTarget != highlightMark)
            {
                // 前のターゲットの光を消す
                if (currentHighlightTarget != null) currentHighlightTarget.ToggleHighlight(false);

                // 新しいターゲットを光らせる
                currentHighlightTarget = highlightMark;
                if (currentHighlightTarget != null) currentHighlightTarget.ToggleHighlight(true);
            }

            // ==========================================
            // ② インタラクトの判定（変更なし）
            // ==========================================
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                UIManager.Instance.ShowInteractPrompt(interactable.GetInteractPrompt());
                if (Input.GetKeyDown(KeyCode.E)) interactable.Interact();
            }
            else
            {
                UIManager.Instance.ShowInteractPrompt("");
            }
        }
        else
        {
            // ==========================================
            // ③ 何も見ていない時の処理
            // ==========================================
            UIManager.Instance.ShowInteractPrompt("");

            if (currentHighlightTarget != null)
            {
                currentHighlightTarget.ToggleHighlight(false);
                currentHighlightTarget = null;
            }
        }
    }
}