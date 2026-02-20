using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    [Header("このキャラの会話データ")]
    public DialogueData myDialogue; // ここにStep1で作ったファイルをセットする

    public string GetInteractPrompt()
    {
        return "話しかける";
    }

    public void Interact()
    {
        // マネージャーに会話データを渡して再生開始！
        if (myDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(myDialogue);
        }
    }
}