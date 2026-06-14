using UnityEngine;
using System.Collections;

// IInteractable を継承
public class DoorController : MonoBehaviour, IInteractable
{
    public enum DoorType { Normal, RequiresKey, RequiresFlag, Broken }
    public DoorType doorType;

    [Header("鍵が必要な場合のみセット")]
    public ItemData requiredKey;

    [Header("イベント連携（オプション）")]
    [Tooltip("このドアを調べた時に会話などを起こす場合、合言葉を入力（空欄なら通常のドア）")]
    public string myInteractID;

    [Tooltip("このドアを調べた瞬間にONにしたいフラグ名（空欄なら何もしない）")]
    public string setFlagOnInteract;

    [Header("フラグが必要な場合のみセット")]
    [Tooltip("このフラグがONになっていたら開くようになります")]
    public string requiredFlag;

    // ==========================================
    // 💡 修正：アクセス制限用の変数を追加
    // ==========================================
    [Header("【アクセス制限】")]
    [Tooltip("このフラグがONの時だけインタラクトできるようにします（空欄ならいつでも可）")]
    public string requiredFlagToInteract;

    [Header("【メッセージ設定】")]
    [Tooltip("アクセス制限（まだ入るべきでない時）のメッセージ")]
    public string lockedByInteractFlagMessage = "ロックされている。まだ入る必要はなさそうだ。";

    [Tooltip("鍵(Requires Key)を持っていない時のメッセージ")]
    public string lockedByKeyMessage = "ロックされている。特定のキーカードが必要なようだ。";

    [Tooltip("フラグ(Requires Flag)がONになっていない時のメッセージ")]
    public string lockedByFlagMessage = "ロックされている。電力の供給やシステムの操作が必要なようだ。";

    [Tooltip("壊れている(Broken)時のメッセージ")]
    public string brokenMessage = "システムエラー。扉が完全に破損している。";

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

    [Header("サウンド設定")]
    public AudioSource audioSource;
    [Tooltip("動き始めの音")]
    public AudioClip startSound;  
    [Tooltip("動いている最中のループ音")]
    public AudioClip movingSound; 
    [Tooltip("止まった時の音")]
    public AudioClip endSound;    

    private bool isOpen = false;
    private bool isMoving = false;

    private Vector3 mainClosedPos;
    private Vector3 subClosedPos;

    private void Start()
    {
        // ゲーム開始時の閉まっている位置を記憶
        if (mainDoor != null) mainClosedPos = mainDoor.localPosition;
        if (subDoor != null) subClosedPos = subDoor.localPosition;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public string GetInteractPrompt()
    {
        if (isMoving) return ""; 

        if (isOpen)
        {
            return autoClose ? "" : "閉める"; 
        }

        switch (doorType)
        {
            case DoorType.Normal: return "開ける";
            case DoorType.RequiresKey: return "ロック解除";
            case DoorType.RequiresFlag: return "開ける"; 
            case DoorType.Broken: return "調べる";
            default: return "調べる";
        }
    }

    public void Interact()
    {
        if (isMoving) return;

        // 1. フライング防止のアクセス制限チェック
        if (!string.IsNullOrEmpty(requiredFlagToInteract) && GameManager.Instance != null)
        {
            if (!GameManager.Instance.GetFlag(requiredFlagToInteract))
            {
                UIManager.Instance.ShowMessage(lockedByInteractFlagMessage); 
                return; 
            }
        }

        // 2. ドアの開閉処理（消えてしまっていた部分を復旧！）
        if (isOpen && !autoClose)
        {
            StartCoroutine(CloseDoors());
        }
        else if (!isOpen)
        {
            TryOpen();
        }
    }

    private void TryOpen()
    {
        bool isUnlockSuccess = false;

        switch (doorType)
        {
            case DoorType.Normal:
                isUnlockSuccess = true;
                break;

            case DoorType.RequiresKey:
                if (requiredKey != null && InventoryManager.Instance.inventoryList.Contains(requiredKey))
                {
                    UIManager.Instance.ShowMessage("【" + requiredKey.itemName + "】でロックを解除した。");
                    doorType = DoorType.Normal; 
                    isUnlockSuccess = true;
                }
                else
                {
                    UIManager.Instance.ShowMessage(lockedByKeyMessage);
                }
                break;

            case DoorType.RequiresFlag:
                if (!string.IsNullOrEmpty(requiredFlag) && GameManager.Instance.GetFlag(requiredFlag))
                {
                    UIManager.Instance.ShowMessage("ロックが解除された。");
                    doorType = DoorType.Normal; 
                    isUnlockSuccess = true;
                }
                else
                {
                    UIManager.Instance.ShowMessage(lockedByFlagMessage);
                }
                break;

            case DoorType.Broken:
                UIManager.Instance.ShowMessage(brokenMessage);
                break;
        }

        // ロック解除に成功した時だけ、フラグとイベントを発動させる
        if (isUnlockSuccess)
        {
            if (!string.IsNullOrEmpty(myInteractID) && GameManager.Instance != null)
            {
                GameManager.Instance.TriggerInteractEvent(myInteractID);
            }

            if (!string.IsNullOrEmpty(setFlagOnInteract) && GameManager.Instance != null)
            {
                GameManager.Instance.SetFlag(setFlagOnInteract, true);
            }

            StartCoroutine(OpenDoorsSequence());
        }
    }

    private IEnumerator OpenDoorsSequence()
    {
        isOpen = true;
        
        yield return StartCoroutine(MoveDoors(true));

        if (autoClose)
        {
            yield return new WaitForSeconds(autoCloseDelay); 
            
            if (isOpen) 
            {
                yield return StartCoroutine(MoveDoors(false)); 
                isOpen = false;
            }
        }
    }

    private IEnumerator CloseDoors()
    {
        yield return StartCoroutine(MoveDoors(false));
        isOpen = false;
    }

    private IEnumerator MoveDoors(bool isOpening)
    {
        isMoving = true;
        float timeElapsed = 0;

        if (audioSource != null && startSound != null)
        {
            audioSource.PlayOneShot(startSound);
        }

        if (audioSource != null && movingSound != null)
        {
            audioSource.clip = movingSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        Vector3 mainTarget = isOpening ? mainClosedPos + slideOffset : mainClosedPos;
        Vector3 subTarget = isOpening ? subClosedPos - slideOffset : subClosedPos;

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

        if (mainDoor != null) mainDoor.localPosition = mainTarget;
        if (subDoor != null) subDoor.localPosition = subTarget;

        if (audioSource != null)
        {
            audioSource.Stop();       
            audioSource.loop = false; 

            if (endSound != null)
            {
                audioSource.PlayOneShot(endSound);
            }
        }

        isMoving = false;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInteractPrompt(GetInteractPrompt());
        }
    }
}