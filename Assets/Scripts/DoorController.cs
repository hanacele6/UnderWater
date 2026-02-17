using UnityEngine;
using System.Collections;

// IInteractable を継承
public class DoorController : MonoBehaviour, IInteractable
{
    public enum DoorType { Normal, RequiresKey, Broken }
    public DoorType doorType;

    [Header("鍵が必要な場合のみセット")]
    public ItemData requiredKey;

    [Header("ドアの割り当て(main:左、sub:右)")]
    [Tooltip("片開きの場合はここだけセットしてください（通常は左側）")]
    public Transform mainDoor;
    [Tooltip("両開きの場合のみセットしてください（メインドアと逆に動きます）")]
    public Transform subDoor;

    [Header("スライド設定")]
    [Tooltip("メインドアが開く時に移動する距離と方向")]
    public Vector3 slideOffset = new Vector3(2f, 0f, 0f); 
    public float slideDuration = 1.5f;

    [Header("自動で閉まる設定")]
    [Tooltip("チェックを入れると、開いた後に自動で閉まります")]
    public bool autoClose = false;
    [Tooltip("開ききってから閉まり始めるまでの待機時間（秒）")]
    public float autoCloseDelay = 3.0f;

    private bool isOpen = false;
    private bool isMoving = false;

    private Vector3 mainClosedPos;
    private Vector3 subClosedPos;

    private void Start()
    {
        // ゲーム開始時の閉まっている位置を記憶
        if (mainDoor != null) mainClosedPos = mainDoor.localPosition;
        if (subDoor != null) subClosedPos = subDoor.localPosition;
    }

    // 視線を合わせた時のテキスト表示
    public string GetInteractPrompt()
    {
        if (isMoving) return ""; // 動いている最中は何も表示しない

        if (isOpen)
        {
            // 開いている時、自動で閉まるドアなら何も出さない。手動なら「閉める」と出す
            return autoClose ? "" : "閉める"; 
        }

        switch (doorType)
        {
            case DoorType.Normal: return "開ける";
            case DoorType.RequiresKey: return "ロック解除";
            case DoorType.Broken: return "調べる";
            default: return "調べる";
        }
    }

    public void Interact()
    {
        if (isMoving) return;

        if (isOpen && !autoClose)
        {
            // 手動設定で、すでに開いているなら「閉める」処理を実行
            StartCoroutine(CloseDoors());
        }
        else if (!isOpen)
        {
            // 閉まっているなら「開ける」判定へ
            TryOpen();
        }
    }

    private void TryOpen()
    {
        switch (doorType)
        {
            case DoorType.Normal:
                UIManager.Instance.ShowMessage("ドアが開いた。");
                StartCoroutine(OpenDoorsSequence());
                break;

            case DoorType.RequiresKey:
                if (InventoryManager.Instance.inventoryList.Contains(requiredKey))
                {
                    UIManager.Instance.ShowMessage("【" + requiredKey.itemName + "】でロックを解除した。");
                    StartCoroutine(OpenDoorsSequence());
                }
                else
                {
                    UIManager.Instance.ShowMessage("ロックされている。特定のキーカードが必要なようだ。");
                }
                break;

            case DoorType.Broken:
                UIManager.Instance.ShowMessage("システムエラー。電力の供給が絶たれている。");
                break;
        }
    }

    // 開く→（必要なら）待つ→閉まる という一連の流れ
    private IEnumerator OpenDoorsSequence()
    {
        isOpen = true;
        
        // ① ドアを開けるアニメーション（完了するまでここで待機）
        yield return StartCoroutine(MoveDoors(true));

        // ② 自動で閉まる設定がONなら
        if (autoClose)
        {
            yield return new WaitForSeconds(autoCloseDelay); // 設定した秒数だけ待機
            
            if (isOpen) // 待っている間に何らかの理由で状態が変わっていなければ
            {
                yield return StartCoroutine(MoveDoors(false)); // ドアを閉める
                isOpen = false;
            }
        }
    }

    // 手動で閉める用
    private IEnumerator CloseDoors()
    {
        yield return StartCoroutine(MoveDoors(false));
        isOpen = false;
    }

    // ドアを動かす共通処理（isOpeningが true なら開く、false なら閉める）
    private IEnumerator MoveDoors(bool isOpening)
    {
        isMoving = true;
        float timeElapsed = 0;

        // 目標位置の計算（開く時はOffsetを足し、閉める時は元の位置に戻す）
        Vector3 mainTarget = isOpening ? mainClosedPos + slideOffset : mainClosedPos;
        Vector3 subTarget = isOpening ? subClosedPos - slideOffset : subClosedPos;

        // 現在位置の取得
        Vector3 mainStart = mainDoor != null ? mainDoor.localPosition : Vector3.zero;
        Vector3 subStart = subDoor != null ? subDoor.localPosition : Vector3.zero;

        while (timeElapsed < slideDuration)
        {
            float t = timeElapsed / slideDuration;

            if (mainDoor != null) mainDoor.localPosition = Vector3.Lerp(mainStart, mainTarget, t);
            if (subDoor != null) subDoor.localPosition = Vector3.Lerp(subStart, subTarget, t);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // ズレを補正
        if (mainDoor != null) mainDoor.localPosition = mainTarget;
        if (subDoor != null) subDoor.localPosition = subTarget;

        isMoving = false;

        // 動いた直後に、画面のプロンプトテキスト（開ける/閉める）を更新させる
        UIManager.Instance.ShowInteractPrompt(GetInteractPrompt());
    }
}