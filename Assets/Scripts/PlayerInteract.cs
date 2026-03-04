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

        if (Physics.Raycast(ray, out hit, interactDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // ==========================================
            // ① アウトラインの判定 (視線を合わせていることを伝える)
            // ==========================================
            InteractableHighlight highlightMark = hit.collider.GetComponentInParent<InteractableHighlight>();

            if (currentHighlightTarget != highlightMark)
            {
                // 前まで見ていたオブジェクトの視線判定を解除
                if (currentHighlightTarget != null) currentHighlightTarget.SetGaze(false);
                
                currentHighlightTarget = highlightMark;
                
                // 新しく見たオブジェクトに視線判定を付与
                if (currentHighlightTarget != null) currentHighlightTarget.SetGaze(true);
            }

            // ==========================================
            // ② インタラクトの判定
            // ==========================================
            IInteractable[] interactables = hit.collider.GetComponentsInParent<IInteractable>();
            
            if (interactables.Length > 0)
            {
                UIManager.Instance.ShowInteractPrompt(interactables[0].GetInteractPrompt());
                
                if (Input.GetKeyDown(KeyCode.E))
                {
                    foreach (var interactable in interactables)
                    {
                        interactable.Interact(); 
                    }

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
                // 見るのをやめたことを伝える
                currentHighlightTarget.SetGaze(false);
                currentHighlightTarget = null;
            }
        }
    }
}