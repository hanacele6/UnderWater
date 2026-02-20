using UnityEngine;

public class Bed : MonoBehaviour, IInteractable
{
    public string GetInteractPrompt()
    {
        // 夜（FreeTime）の時だけ「寝る」と表示する
        if (GameManager.Instance.currentPhase == GamePhase.FreeTime)
        {
            return "ベッドで休む（翌日へ）";
        }
        return "今はまだ眠くない";
    }

    public void Interact()
    {
        // 夜なら次の日へ！
        if (GameManager.Instance.currentPhase == GamePhase.FreeTime)
        {
            GameManager.Instance.NextPhase();
        }
    }
}