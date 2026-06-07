using UnityEngine;
using System.Collections.Generic;
public enum ItemCategory
{
    Material,   // 素材
    Sample,     // サンプル
    Document,   // 文献
    Goods,      // 物品
    Valuable    // 貴重品
}

public enum ItemTag
{
    None,
    Meat,           // 肉組織
    Plant,          // 植物・菌類
    Toxic,          // 毒性
    Bioluminescent, // 発光
    Armored,        // 装甲・硬質
    Electric,       // 帯電
    Anomalous       // 異常物質（謎の成分）
}

public enum EquipSlotType
{
    None,           // 装備不可のアイテム
    Generator,      // メインジェネレーター用
    Filter,         // サブフィルター用
    HullArmor,      // 装甲材
    SonarModule     // ソナーモジュール
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

    [Header("バイオ調合設定")]
    [Tooltip("このアイテムを鍋に入れた時に追加される属性")]
    public List<ItemTag> itemTags = new List<ItemTag>();

    [Tooltip("この成分の濃さ（通常は1。強力なレア素材なら2や3にする）")]
    public int potency = 1;

    [Header("3D設定")]
    [Tooltip("フラスコに投入した時に落下する3Dモデルのプレハブ")]
    public GameObject dropPrefab;

    [Header("調合時の色設定")]
    [Tooltip("フラスコに投入した時に液体に混ざる色")]
    public Color materialColor = Color.white;

    [Header("潜水艦装備設定")]
    [Tooltip("装備可能な部位。Noneなら装備不可")]
    public EquipSlotType equipType = EquipSlotType.None;

    // 💡 単一のパワーではなく、効果の組み合わせリストに変更！
    [Tooltip("このアイテムを装備した時に発動する効果のリスト（複数設定可能）")]
    public List<SubmarineEffect> equipEffects = new List<SubmarineEffect>();
}