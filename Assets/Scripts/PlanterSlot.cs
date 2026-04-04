using UnityEngine;
using System.Collections.Generic;

public class PlanterSlot : MonoBehaviour
{
    [Header("スロットの状態")]
    public bool isPlanted = false;
    public bool isReadyToHarvest = false;

    [Header("3Dモデル設定")]
    public Transform plantSpawnPoint;
    [Tooltip("成長段階ごとのモデル（0:芽, 1:成長中, 2:完成体 など）")]
    public List<GameObject> growthPrefabs = new List<GameObject>();

    private GameObject currentVisual;
    private int currentStage = -1;
    private CultivationRecipeData currentRecipe;
    private int plantedDay; // 植えられた日

    void Start()
    {
        // GameManagerの「OnDayChanged」というスピーカーに、自分の成長メソッドを登録する
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += OnDayChanged;
        }
    }


    public void OnDayChanged(int currentDay)
    {
        if (!isPlanted || isReadyToHarvest) return;

        // 経過日数を計算
        int elapsedDays = currentDay - plantedDay;
        float progress = Mathf.Clamp01((float)elapsedDays / currentRecipe.daysToGrow);

  
        // 例：3段階のモデルがあるなら、0~33%はStage0、34~66%はStage1...
        int stageCount = growthPrefabs.Count;
        int targetStage = Mathf.Min(Mathf.FloorToInt(progress * stageCount), stageCount - 1);

        if (targetStage != currentStage)
        {
            UpdateVisual(targetStage);
        }

        // 💡 スケールの計算（各段階の中で 0.5 ~ 1.0 に膨らむなど）
        if (currentVisual != null)
        {
            float stageProgress = (progress * stageCount) % 1.0f;
            if (progress >= 1.0f) stageProgress = 1.0f;
            float scale = Mathf.Lerp(0.5f, 1.0f, stageProgress);
            currentVisual.transform.localScale = Vector3.one * scale;
        }

        if (elapsedDays >= currentRecipe.daysToGrow)
        {
            isReadyToHarvest = true;
        }
    }

    private void UpdateVisual(int stageIndex)
    {
        if (currentVisual != null) Destroy(currentVisual);
        
        currentStage = stageIndex;
        if (growthPrefabs[stageIndex] != null)
        {
            currentVisual = Instantiate(growthPrefabs[stageIndex], plantSpawnPoint.position, Quaternion.identity, plantSpawnPoint);
            currentVisual.transform.localScale = Vector3.one * 0.5f;
        }
    }

    public void PlantSeed(CultivationRecipeData recipe, int currentDay)
    {
        currentRecipe = recipe;
        plantedDay = currentDay;
        isPlanted = true;
        isReadyToHarvest = false;
        UpdateVisual(0); // 最初の段階（芽）を表示
    }
}