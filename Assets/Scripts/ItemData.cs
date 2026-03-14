using UnityEngine;

public enum ItemCategory
{
    Goods,      // 物品
    Document,   // 文献
    Material,   // 素材
    Valuable    // 貴重品
}

// 右クリックメニューからこのデータを作成できるようにする魔法の1行
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName; // アイテムの名前
    [TextArea(3, 5)]
    public string description; // 詳細な説明テキスト
    public Sprite itemIcon; // 後UIにアイコンを表示したい時用

    [Header("分類")]
    public ItemCategory category = ItemCategory.Material;
}