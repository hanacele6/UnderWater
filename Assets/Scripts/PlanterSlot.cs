using UnityEngine;
using System.Collections.Generic;

public class PlanterSlot : MonoBehaviour
{
    [Header("スロットの状態")]
    public bool isPlanted = false;
    public bool isReadyToHarvest = false;

    [Header("3Dモデル設定")]
    public Transform plantSpawnPoint;
    
    [Tooltip("※現在はレシピ側のデータを使用します")]
    public List<GameObject> growthPrefabs = new List<GameObject>();

    private GameObject currentVisual;
    private int currentStage = -1;
    private CultivationRecipeData currentRecipe;
    private int plantedDay; // 植えられた日

    void Start()
    {
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

        int stageCount = currentRecipe.growthPrefabs.Count;
        int targetStage = Mathf.Min(Mathf.FloorToInt(progress * stageCount), stageCount - 1);

        if (targetStage != currentStage)
        {
            UpdateVisual(targetStage);
        }

        // 成長に合わせてスケールを更新
        if (currentVisual != null)
        {
            float multiplier = Mathf.Lerp(currentRecipe.startScaleMultiplier, currentRecipe.endScaleMultiplier, progress);
            GameObject originalPrefab = currentRecipe.growthPrefabs[currentStage];
            currentVisual.transform.localScale = originalPrefab.transform.localScale * multiplier;
        }

        if (elapsedDays >= currentRecipe.daysToGrow)
        {
            isReadyToHarvest = true;
        }
    }

    void OnMouseDown()
{
    if (UnityEngine.EventSystems.EventSystem.current != null && 
        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

    if (isReadyToHarvest)
    {
        CultivatorConsole myConsole = GetComponentInParent<CultivatorConsole>();
        
        // 自分のコンソールについているスロット「だけ」を取得
        // （もし単体で置かれていた場合は、自分だけを配列に入れる安全設計）
        PlanterSlot[] mySlots = myConsole != null 
            ? myConsole.GetComponentsInChildren<PlanterSlot>() 
            : new PlanterSlot[] { this }; 

        List<ItemData> totalHarvestResults = new List<ItemData>();

        foreach (var slot in mySlots)
        {
            if (slot.isReadyToHarvest)
            {
                SampleItemData item = slot.Harvest(); 
                if (item != null)
                {
                    // ※ここではインベントリに入れない！（UIのTakeAllItemsで入れるため）
                    totalHarvestResults.Add(item);
                }
            }
        }

        if (totalHarvestResults.Count > 0)
        {
            if (ExtractionUIManager.Instance != null)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.isUIOpen = true;
                    GameManager.Instance.LockPlayer(); // カーソルを出す
                }

                // UIを開き、閉じた時の「後片付け」を予約する
                ExtractionUIManager.Instance.OpenExtractionUI(totalHarvestResults, () => 
                {
                    if (GameManager.Instance != null)
                    {
                        // UnlockPlayer は UIManager 側でやってくれるので、
                        // ここでは「UI開いてるよフラグ」だけを確実に下ろす！
                        GameManager.Instance.isUIOpen = false;
                    }
                });
            }
        }
    }
    else if (!isPlanted)
    {
        // 🌱 種まき時も、Instance(全体)ではなく「自分の親コンソール」を呼ぶように変更！
        // これで複数のコンソールを並べても完璧に動きます。
        CultivatorConsole myConsole = GetComponentInParent<CultivatorConsole>();
        if (myConsole != null)
        {
            myConsole.OpenSeedSelectionUI(this);
        }
    }
    else
    {
        Debug.Log("⏳ まだ成長中です……。");
    }
}
    
    public SampleItemData Harvest()
    {
        if (!isReadyToHarvest || currentRecipe == null) return null;

        SampleItemData harvestedItem = currentRecipe.finalSample; 

        isPlanted = false;
        isReadyToHarvest = false;
        currentRecipe = null;
        plantedDay = 0;
        currentStage = -1;
        
        if (currentVisual != null) 
        {
            Destroy(currentVisual);
            currentVisual = null;
        }

        return harvestedItem; 
    }

    private void UpdateVisual(int stageIndex)
    {
        if (currentRecipe == null || currentRecipe.growthPrefabs == null || currentRecipe.growthPrefabs.Count == 0) return;

        GameObject prefabToSpawn = currentRecipe.growthPrefabs[stageIndex];
        if (prefabToSpawn == null) return;

        if (currentVisual != null) Destroy(currentVisual);
        currentStage = stageIndex;

        //currentVisual = Instantiate(prefabToSpawn, plantSpawnPoint.position, Quaternion.identity, plantSpawnPoint);
        currentVisual = Instantiate(prefabToSpawn, plantSpawnPoint.position, prefabToSpawn.transform.rotation, plantSpawnPoint);
        
    }

    public void PlantSeed(CultivationRecipeData recipe, int currentDay)
    {
        currentRecipe = recipe;
        plantedDay = currentDay;
        isPlanted = true;
        isReadyToHarvest = false;
        
        UpdateVisual(0); 

        if (currentVisual != null && currentRecipe.growthPrefabs.Count > 0)
        {
            float multiplier = currentRecipe.startScaleMultiplier;
            GameObject originalPrefab = currentRecipe.growthPrefabs[0];
            currentVisual.transform.localScale = originalPrefab.transform.localScale * multiplier;
        }
    }
}