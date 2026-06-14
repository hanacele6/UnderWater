using UnityEngine;

public class OneTimeInteractable : MonoBehaviour, IInteractable
{
    public enum InteractMode { EventOnly, PickupOnly, Both }
    
    [Header("【モード設定】")]
    public InteractMode interactMode = InteractMode.EventOnly;

    [Header("【アイテム情報】")]
    public ItemData itemData;

    [Header("【イベント連携】")]
    public string myInteractID;

    [Header("【直接フラグ操作】")]
    public string setFlagOnInteract;


    [Header("【アクセス制限】")]
    [Tooltip("このフラグがONの時だけインタラクトできるようにします（空欄ならいつでも可）")]
    public string requiredFlagToInteract;
    [Tooltip("ロックされている時に画面に出る文字")]
    public string lockedPromptMessage = "今はまだ調べる必要はなさそうだ";
    
    [Header("【プロンプト設定】")]
    public string promptMessage = "調べる";

    [Header("【演出設定】")]
    public AudioClip interactSound;
    public GameObject effectPrefab;

    [Header("【インタラクト後の処理】")]
    public bool destroyAfterInteract = true;

    private bool hasInteracted = false; 

    // 💡 ロック状態かどうかを判定する便利メソッド
    private bool IsLocked()
    {
        if (string.IsNullOrEmpty(requiredFlagToInteract)) return false; // 条件なし
        if (GameManager.Instance == null) return false;
        return !GameManager.Instance.GetFlag(requiredFlagToInteract); // フラグが未達成ならロック
    }

    public string GetInteractPrompt()
    {
        if (hasInteracted) return "";

        // 💡 ロックされている時は専用のプロンプトを出す
        if (IsLocked()) return lockedPromptMessage;

        if (itemData != null && (interactMode == InteractMode.PickupOnly || interactMode == InteractMode.Both))
        {
            return $"[{itemData.itemName}] を{promptMessage}";
        }
        
        return promptMessage;
    }

    public void Interact()
    {
        if (hasInteracted) return;

        // 💡 ロックされている時はインタラクト処理をさせない
        if (IsLocked()) return;

        // 1. アイテム取得処理
        if (itemData != null && (interactMode == InteractMode.PickupOnly || interactMode == InteractMode.Both))
        {
            InventoryManager.Instance.AddItem(itemData);
            if (itemData.category == ItemCategory.Material) UIManager.Instance.ShowMessage($"【{itemData.itemName}】 を手に入れた");
            else UIManager.Instance.ShowItemPickupDetail(itemData);
        }

        // 2. サウンド再生
        if (interactSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySE(interactSound);

        // 3. エフェクト生成
        if (effectPrefab != null) Instantiate(effectPrefab, transform.position, Quaternion.identity);

        // 4. イベント進行
        if (!string.IsNullOrEmpty(myInteractID) && (interactMode == InteractMode.EventOnly || interactMode == InteractMode.Both))
        {
            GameManager.Instance.TriggerInteractEvent(myInteractID);
        }

        // 5. 直接フラグを立てる
        if (!string.IsNullOrEmpty(setFlagOnInteract))
        {
            GameManager.Instance.SetFlag(setFlagOnInteract, true);
        }

        hasInteracted = true;
        if (UIManager.Instance != null) UIManager.Instance.ShowInteractPrompt(""); 

        if (destroyAfterInteract) Destroy(gameObject);
    }
}