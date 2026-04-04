using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;

public class FlaskReceiver : MonoBehaviour
{
    public static FlaskReceiver Instance;

    [Header("フラスコの設定（3D液体）")]
    public Renderer liquidRenderer; 
    public Transform openingPoint; 
    public Transform bottomPoint;    
    public float currentLiquidAmount = 0f;
    public float maxLiquidAmount = 100f;

    public bool isMixingComplete = false;
    public bool IsFull => currentLiquidAmount >= (maxLiquidAmount - 5.0f);

    [Header("フラスコの内容物（固体・成分）")]
    public List<ItemData> addedItems = new List<ItemData>();
    public Dictionary<ItemTag, int> currentTags = new Dictionary<ItemTag, int>();

    [Header("遠心分離・かき混ぜ設定")]
    [Range(0, 100)]
    public float mixProgress = 0f;
    public GameObject progressBarRoot; 
    public Image progressFillImage;    

    [Header("完成品データベース")]
    public List<MixRecipeData> allRecipes = new List<MixRecipeData>();
    public MixRecipeData defaultSludge; 
    
    public MixRecipeData completedResult = null;

    private const string FillLevelProp = "_FillLevel";
    private Material liquidMat;

    [Header("エフェクト設定")]
    public ParticleSystem bubbleParticles;
    public float colorMixDuration = 2.0f;

    private Color defaultColor = new Color(0.2f, 1.0f, 1.0f, 1.0f); 
    private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
    private static readonly int TargetColorProp = Shader.PropertyToID("_TargetColor");
    private static readonly int MarbleProgressProp = Shader.PropertyToID("_MarbleProgress");

    private Coroutine mixColorCoroutine;

    public float pourRatePerSecond = 20f;

    private void Awake()
    {
        Instance = this; 
    }

    private void Start()
    {
        if (progressBarRoot != null) progressBarRoot.gameObject.SetActive(false);

        isMixingComplete = false; 
        mixProgress = 0f;

        if (liquidRenderer != null)
        {
            liquidMat = liquidRenderer.material;
            if (liquidMat != null)
            {
                liquidMat.SetColor(BaseColorProp, defaultColor);
                liquidMat.SetColor(TargetColorProp, defaultColor);
                liquidMat.SetFloat(MarbleProgressProp, 1f); 
            }
            currentLiquidAmount = 0f; 
            UpdateShaderFillLevel(); 
        }
    }

    private void LateUpdate() 
    {
        if (liquidMat != null && bottomPoint != null && openingPoint != null)
        {
            UpdateShaderFillLevel();
        }
    }

    public void ReceiveLiquid(float amount)
    {
        if (IsFull || isMixingComplete) return;
        currentLiquidAmount += amount;
        currentLiquidAmount = Mathf.Clamp(currentLiquidAmount, 0, maxLiquidAmount);
    }

    private void UpdateShaderFillLevel()
    {
        if (liquidMat != null && bottomPoint != null && openingPoint != null)
        {
            if (currentLiquidAmount <= 0.01f) 
            {
                liquidMat.SetFloat(FillLevelProp, bottomPoint.position.y - 5.0f);
                return;
            }

            float ratio = currentLiquidAmount / maxLiquidAmount;
            float currentWorldY = Mathf.Lerp(bottomPoint.position.y, openingPoint.position.y, ratio);
            liquidMat.SetFloat(FillLevelProp, currentWorldY);
        }
    }

    public void AddIngredient(ItemData item)
    {
        if (isMixingComplete) return;

        addedItems.Add(item);

        if (bubbleParticles != null) bubbleParticles.Play();

        Color totalColor = Color.clear;
        foreach (var added in addedItems)
        {
            totalColor += added.materialColor;
        }
        Color targetColor = totalColor / addedItems.Count;

        if (mixColorCoroutine != null) StopCoroutine(mixColorCoroutine);
        mixColorCoroutine = StartCoroutine(SmoothColorMix(targetColor));

        foreach (ItemTag tag in item.itemTags)
        {
            if (currentTags.ContainsKey(tag)) currentTags[tag] += item.potency;
            else currentTags.Add(tag, item.potency);
        }
    }

    private IEnumerator SmoothColorMix(Color targetColor)
    {
        Color currentBase = liquidMat.GetColor(TargetColorProp); 
        
        if (liquidMat != null)
        {
            liquidMat.SetColor(BaseColorProp, currentBase);
            liquidMat.SetColor(TargetColorProp, targetColor);
            liquidMat.SetFloat(MarbleProgressProp, 0f); 
        }

        float elapsed = 0f;
        while (elapsed < colorMixDuration)
        {
            elapsed += Time.deltaTime;
            float rawProgress = elapsed / colorMixDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, rawProgress);
            smoothProgress = Mathf.SmoothStep(0f, 1f, smoothProgress); 

            if (liquidMat != null) liquidMat.SetFloat(MarbleProgressProp, smoothProgress);
            yield return null; 
        }

        if (liquidMat != null) liquidMat.SetFloat(MarbleProgressProp, 1f); 
    }

    public void SetProgressBarVisible(bool isVisible)
    {
        if (isMixingComplete && isVisible) return;
        if (progressBarRoot != null) progressBarRoot.SetActive(isVisible);
    }
    
    private bool isProcessingSynthesis = false;
    
    public void AddMixProgress(float amount)
    {
        if (currentLiquidAmount <= 0.1f || addedItems.Count == 0 || isProcessingSynthesis || isMixingComplete) return;

        mixProgress += amount;
        mixProgress = Mathf.Clamp(mixProgress, 0, 100f);

        if (progressFillImage != null) progressFillImage.fillAmount = mixProgress / 100f;

        if (mixProgress >= 100f) 
        {
            isProcessingSynthesis = true; 
            CompleteSynthesis();
        }
    }

    private void CompleteSynthesis()
    {
        SetProgressBarVisible(false);

        try
        {
            MixRecipeData result = null;
            int highestPriority = -1;

            if (allRecipes != null)
            {
                foreach (var recipe in allRecipes)
                {
                    if (recipe != null && CheckRecipeCondition(recipe))
                    {
                        if (recipe.priority > highestPriority)
                        {
                            result = recipe;
                            highestPriority = recipe.priority;
                        }
                    }
                }
            }

            if (result == null) result = defaultSludge;

            completedResult = result;

            if (result != null)
            {
                Debug.Log($"✨ 完成したタネ：【{result.recipeName}】 ✨");
                
                Color resultColor = result.potionColor;
                
                if (mixColorCoroutine != null) StopCoroutine(mixColorCoroutine);
                mixColorCoroutine = StartCoroutine(SmoothColorMix(resultColor));
                
                if (bubbleParticles != null) bubbleParticles.Play(); 
            }
            else
            {
                Debug.LogWarning("注意：インスペクターの Default Sludge が設定されていません！");
            }
            
            isMixingComplete = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"調合の判定中にエラーが発生しましたが、処理を続行します: {e.Message}");
        }
        finally
        {
            mixProgress = 0f;
            if (progressFillImage != null) progressFillImage.fillAmount = 0f;
            isProcessingSynthesis = false;
        }
    }

    public void EmptyFlask()
    {
        addedItems.Clear();
        currentTags.Clear();
        currentLiquidAmount = 0f;
        completedResult = null;
        isMixingComplete = false;
        mixProgress = 0f;

        UpdateShaderFillLevel();

        if (mixColorCoroutine != null) StopCoroutine(mixColorCoroutine);
        mixColorCoroutine = StartCoroutine(SmoothColorMix(defaultColor));

        Debug.Log("フラスコが空になりました！次の調合が可能です。");
    }

    private bool CheckRecipeCondition(MixRecipeData recipe)
    {
        if (recipe == null) return false;

        if (recipe.forbiddenTags != null)
        {
            foreach (var badTag in recipe.forbiddenTags)
            {
                if (badTag != null && currentTags.ContainsKey(badTag) && currentTags[badTag] > 0) return false;
            }
        }
        if (recipe.requiredTags != null)
        {
            foreach (var req in recipe.requiredTags)
            {
                if (req.requiredTag == null) continue;
                if (!currentTags.ContainsKey(req.requiredTag)) return false; 
                if (currentTags[req.requiredTag] < req.requiredAmount) return false; 
            }
        }
        return true; 
    }

    private void OnTriggerEnter(Collider other)
    {
        IngredientObject droppedIngredient = other.GetComponent<IngredientObject>();
        if (droppedIngredient != null && droppedIngredient.ingredientData != null)
        {
            AddIngredient(droppedIngredient.ingredientData);
            Destroy(other.gameObject);
        }
    }
}