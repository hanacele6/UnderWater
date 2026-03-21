using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FlaskReceiver : MonoBehaviour
{
    public static FlaskReceiver Instance;

    [Header("フラスコの設定（液体）")]
    public Image flaskLiquidUI;
    public RectTransform openingPoint;
    public float currentLiquidAmount = 0f;
    public float maxLiquidAmount = 100f;
    public bool IsFull => currentLiquidAmount >= maxLiquidAmount;

    [Header("フラスコの内容物（固体・成分）")]
    public List<ItemData> addedItems = new List<ItemData>();
    public Dictionary<ItemTag, int> currentTags = new Dictionary<ItemTag, int>();

    [Header("遠心分離・かき混ぜ設定")]
    [Range(0, 100)]
    public float mixProgress = 0f;
    public Image mixProgressUI; // もしプログレスバーを出すなら（アサインしなくてもOK）

    [Header("完成品データベース")]
    public List<GrownSampleData> allRecipes = new List<GrownSampleData>();
    public GrownSampleData defaultSludge; 

    private void Awake()
    {
        Instance = this;
        if (flaskLiquidUI != null) flaskLiquidUI.fillAmount = 0f;
    }

    // ==========================================
    // 1. 液体（特殊細菌）を受け取る
    // ==========================================
    public void ReceiveLiquid(float amount)
    {
        if (IsFull) return;

        currentLiquidAmount += amount;
        currentLiquidAmount = Mathf.Clamp(currentLiquidAmount, 0, maxLiquidAmount);

        if (flaskLiquidUI != null) flaskLiquidUI.fillAmount = currentLiquidAmount / maxLiquidAmount;

        if (IsFull) Debug.Log("フラスコに特殊細菌が満たされました！");
    }

    // ==========================================
    // 2. 素材（アイテム）を受け取る
    // ==========================================
    public void AddIngredient(ItemData item)
    {
        addedItems.Add(item);

        foreach (ItemTag tag in item.itemTags)
        {
            if (currentTags.ContainsKey(tag)) currentTags[tag] += item.potency;
            else currentTags.Add(tag, item.potency);
        }

        Debug.Log($"フラスコに {item.itemName} を投入した！");
    }

    // ==========================================
    // 3. ぐるぐる回す（遠心分離/撹拌）
    // ==========================================
    public void AddMixProgress(float amount)
    {
        // 液体が満タンじゃない、または素材が1つも入っていない時は反応しない！
        if (!IsFull || addedItems.Count == 0) return;

        mixProgress += amount;
        mixProgress = Mathf.Clamp(mixProgress, 0, 100f);

        if (mixProgressUI != null) mixProgressUI.fillAmount = mixProgress / 100f;

        // ★ここでフラスコの中身の色を少しずつ変えたり、ブクブク泡立たせると最高です

        if (mixProgress >= 100f) CompleteSynthesis();
    }

    // ==========================================
    // 4. 調合完了時の判定
    // ==========================================
    private void CompleteSynthesis()
    {
        Debug.Log("化学反応完了！！");
        mixProgress = 0f; 

        GrownSampleData result = null;
        int highestPriority = -1;

        foreach (var recipe in allRecipes)
        {
            if (CheckRecipeCondition(recipe))
            {
                if (recipe.priority > highestPriority)
                {
                    result = recipe;
                    highestPriority = recipe.priority;
                }
            }
        }

        if (result == null) result = defaultSludge;

        Debug.Log($"完成したタネ：【{result.sampleName}】（培養に {result.daysToGrow} 日かかります）");

        // ★ここでインベントリか、培養ポッドにタネを送る処理！

        // 終わったらフラスコを空っぽにする
        addedItems.Clear();
        currentTags.Clear();
        currentLiquidAmount = 0f;
        if (flaskLiquidUI != null) flaskLiquidUI.fillAmount = 0f;
    }

    private bool CheckRecipeCondition(GrownSampleData recipe)
    {
        foreach (var badTag in recipe.forbiddenTags)
        {
            if (currentTags.ContainsKey(badTag) && currentTags[badTag] > 0) return false;
        }

        foreach (var req in recipe.requiredTags)
        {
            if (!currentTags.ContainsKey(req.requiredTag)) return false; 
            if (currentTags[req.requiredTag] < req.requiredAmount) return false; 
        }
        return true; 
    }
}