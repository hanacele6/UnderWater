using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;

    void Update()
    {
        Camera mainCamera = Camera.main;
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // ★ EvidenceItem や DoorController を個別に探すのではなく、
            // 「IInteractable（調べる機能）」を持っているかだけを確認する！
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                // ① 対象からプロンプトのテキストをもらって、画面に表示する
                UIManager.Instance.ShowInteractPrompt(interactable.GetInteractPrompt());

                // ② Eキーを押したら、対象の Interact() を実行する
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();
                }
            }
            else
            {
                // 何も持っていない壁などを見ている時は消す（空文字を渡す）
                UIManager.Instance.ShowInteractPrompt("");
            }
        }
        else
        {
            // 何も見ていない時は消す
            UIManager.Instance.ShowInteractPrompt("");
        }
    }
}