using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FlaskReceiver : MonoBehaviour
{
    public static FlaskReceiver Instance;

    [Header("フラスコの設定（3D液体）")]
    [Tooltip("フラスコの中の液体メッシュをアサイン")]
    public Renderer liquidRenderer; 
    [Tooltip("フラスコの口（注がれる判定の位置）")]
    public Transform openingPoint; 
    
    public float currentLiquidAmount = 0f;
    public float maxLiquidAmount = 100f;
    public bool IsFull => currentLiquidAmount >= maxLiquidAmount;

    [Header("フラスコの内容物（固体・成分）")]
    public List<ItemData> addedItems = new List<ItemData>();
    public Dictionary<ItemTag, int> currentTags = new Dictionary<ItemTag, int>();

    [Header("遠心分離・かき混ぜ設定")]
    [Range(0, 100)]
    public float mixProgress = 0f;
    public Image mixProgressUI; 

    [Header("完成品データベース")]
    public List<GrownSampleData> allRecipes = new List<GrownSampleData>();
    public GrownSampleData defaultSludge; 

    private const string FillLevelProp = "_FillLevel";
    private Material liquidMat;

    private void Awake()
    {
        Instance = this; 
    }

    private void Start()
    {
        if (liquidRenderer != null)
        {
            liquidMat = liquidRenderer.material;
            
            currentLiquidAmount = 0f; 
            UpdateShaderFillLevel(); 
        }
        else
        {
            Debug.LogError("Liquid Renderer");
        }
    }

    // ==========================================
    // 1. 液体（特殊細菌）を受け取る
    // ==========================================
    public void ReceiveLiquid(float amount)
    {
        if (IsFull) return;

        currentLiquidAmount += amount;
        currentLiquidAmount = Mathf.Clamp(currentLiquidAmount, 0, maxLiquidAmount);

        UpdateShaderFillLevel();

        if (IsFull) Debug.Log("フラスコに特殊細菌が満たされました！");
    }

    private void UpdateShaderFillLevel()
    {
        if (liquidMat != null && liquidRenderer != null)
        {
            float ratio = currentLiquidAmount / maxLiquidAmount;
            
            // Unityのバウンディングボックスから一番下と一番上を取得
            float bottomY = liquidRenderer.bounds.min.y;
            float topY = liquidRenderer.bounds.max.y;
            
            // 割合に応じた絶対的な高さを算出
            float currentFillY = Mathf.Lerp(bottomY, topY, ratio);

                       
            liquidMat.SetFloat(FillLevelProp, currentFillY);
        }
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
    // 3. ぐるぐる回す
    // ==========================================
    public void AddMixProgress(float amount)
    {
        if (!IsFull || addedItems.Count == 0) return;

        mixProgress += amount;
        mixProgress = Mathf.Clamp(mixProgress, 0, 100f);

        if (mixProgressUI != null) mixProgressUI.fillAmount = mixProgress / 100f;

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

        addedItems.Clear();
        currentTags.Clear();
        currentLiquidAmount = 0f;
        
        UpdateShaderFillLevel();
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