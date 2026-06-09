using UnityEngine;

public class OneTimeInteractable : MonoBehaviour, IInteractable
{
    public enum InteractMode { EventOnly, PickupOnly, Both }
    
    [Header("【モード設定】")]
    [Tooltip("EventOnly: 会話などのイベントのみ\nPickupOnly: アイテム取得のみ\nBoth: アイテムを取得してイベントも発生")]
    public InteractMode interactMode = InteractMode.EventOnly;

    [Header("【アイテム情報】 (PickupOnly / Both 用)")]
    public ItemData itemData;

    [Header("【イベント連携】 (EventOnly / Both 用)")]
    [Tooltip("GameEventDataで設定した interactTargetID と同じ合言葉を入力")]
    public string myInteractID;
    
    [Header("【プロンプト設定】")]
    [Tooltip("画面に出る文字（例：読む、拾う、調べる）※アイテムがある場合は自動で『[アイテム名] を〜』になります")]
    public string promptMessage = "調べる";

    [Header("【演出設定】")]
    [Tooltip("インタラクトした瞬間に鳴らす効果音")]
    public AudioClip interactSound;
    [Tooltip("インタラクトした瞬間にその場に出すエフェクトのプレハブ")]
    public GameObject effectPrefab;

    [Header("【インタラクト後の処理】")]
    [Tooltip("チェックを入れると、インタラクト後にオブジェクトが消滅します（証拠品の回収など）")]
    public bool destroyAfterInteract = true;

    // 既に実行されたかどうかのガードフラグ
    private bool hasInteracted = false; 

    public string GetInteractPrompt()
    {
        if (hasInteracted) return "";

        // アイテムを拾うモード、かつアイテムデータがある場合は名前付きにする
        if (itemData != null && (interactMode == InteractMode.PickupOnly || interactMode == InteractMode.Both))
        {
            return $"[{itemData.itemName}] を{promptMessage}";
        }
        
        return promptMessage;
    }

    public void Interact()
    {
        if (hasInteracted) return;

        // 1. アイテム取得処理 (PickupOnly または Both の場合)
        if (itemData != null && (interactMode == InteractMode.PickupOnly || interactMode == InteractMode.Both))
        {
            InventoryManager.Instance.AddItem(itemData);
            
            if (itemData.category == ItemCategory.Material)
            {
                UIManager.Instance.ShowMessage($"【{itemData.itemName}】 を手に入れた");
            }
            else
            {
                UIManager.Instance.ShowItemPickupDetail(itemData);
            }
        }

        // 2. サウンド再生（統合版AudioManager経由）
        if (interactSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(interactSound);
        }

        // 3. エフェクト生成
        if (effectPrefab != null)
        {
            Instantiate(effectPrefab, transform.position, Quaternion.identity);
        }

        // 4. イベント進行 (EventOnly または Both の場合)
        if (!string.IsNullOrEmpty(myInteractID) && (interactMode == InteractMode.EventOnly || interactMode == InteractMode.Both))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerInteractEvent(myInteractID);
            }
        }

        // 5. フラグ更新とプロンプト消去
        hasInteracted = true;
        if (UIManager.Instance != null) UIManager.Instance.ShowInteractPrompt(""); 

        // 6. 消滅判定
        if (destroyAfterInteract)
        {
            Destroy(gameObject);
        }
    }
}