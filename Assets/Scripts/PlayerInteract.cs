using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;

    void Update()
    {
        Camera mainCamera = Camera.main;
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        // 何かにぶつかったら
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            EvidenceItem item = hit.collider.GetComponent<EvidenceItem>();
            
            // ぶつかった相手が EvidenceItem だったら
            if (item != null)
            {
                UIManager.Instance.ShowInteractPrompt(true); // 【追加】「[E] 調べる」を表示

                if (Input.GetKeyDown(KeyCode.E))
                {
                    item.Interact();
                    UIManager.Instance.ShowInteractPrompt(false); // 【追加】拾ったら消す
                }
            }
            else
            {
                UIManager.Instance.ShowInteractPrompt(false); // 【追加】アイテムじゃない物を見ている時は消す
            }
        }
        else
        {
            UIManager.Instance.ShowInteractPrompt(false); // 【追加】何も見ていない時は消す
        }
    }
}