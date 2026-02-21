using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;
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

            if (currentHighlightTarget != highlightMark)
            {
                if (currentHighlightTarget != null) currentHighlightTarget.ToggleHighlight(false);
                currentHighlightTarget = highlightMark;
                if (currentHighlightTarget != null) currentHighlightTarget.ToggleHighlight(true);
            }

            // ==========================================
            // ② インタラクトの判定
            // ==========================================
            IInteractable[] interactables = hit.collider.GetComponentsInParent<IInteractable>();
            
            if (interactables.Length > 0)
            {
                // 画面に出すテキストは、とりあえず1つ目のスクリプトのものを採用する
                UIManager.Instance.ShowInteractPrompt(interactables[0].GetInteractPrompt());
                
                if (Input.GetKeyDown(KeyCode.E))
                {
                    // ついている全てのスクリプトの Interact() を順番に全部発動させる！
                    foreach (var interactable in interactables)
                    {
                        interactable.Interact(); 
                    }

                    // GameManagerへのフェーズ移行報告は1回だけでOK
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CheckPhaseTransition(hit.collider.gameObject);
                    }
                }
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