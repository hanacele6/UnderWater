using UnityEngine;

public class PlanterSlot : MonoBehaviour
{
    [Header("スロットの状態")]
    public bool isPlanted = false;
    public bool isReadyToHarvest = false;

    [Header("成長設定")]
    [Tooltip("成長しきるまでの秒数")]
    public float timeToGrow = 10f; 
    private float currentGrowthTimer = 0f;

    [Header("3Dモデル設定")]
    [Tooltip("植物が生える位置")]
    public Transform plantSpawnPoint; 
    [Tooltip("仮の植物プレハブ（種ごとに変えることも可能）")]
    public GameObject defaultPlantPrefab; 
    
    private GameObject currentPlantVisual;
    private ItemData plantedSeedData; // 植えられている種

    void Update()
    {
        // 植えられていて、まだ収穫可能じゃないなら成長タイマーを進める
        if (isPlanted && !isReadyToHarvest)
        {
            currentGrowthTimer += Time.deltaTime;
            
            // 💡 3Dモデルを徐々に大きくする演出（0.1倍から1.0倍へ）
            if (currentPlantVisual != null)
            {
                float scale = Mathf.Lerp(0.1f, 1.0f, currentGrowthTimer / timeToGrow);
                currentPlantVisual.transform.localScale = new Vector3(scale, scale, scale);
            }

            // 成長完了！
            if (currentGrowthTimer >= timeToGrow)
            {
                isReadyToHarvest = true;
                currentPlantVisual.transform.localScale = Vector3.one;
                Debug.Log("🌱 成長が完了しました！収穫可能です。");
            }
        }
    }

    // 種を植えるメソッド（UIから呼ばれる）
    public void PlantSeed(ItemData seedData)
    {
        if (isPlanted) return;

        plantedSeedData = seedData;
        isPlanted = true;
        isReadyToHarvest = false;
        currentGrowthTimer = 0f;

        // 植物の3Dモデルを生成し、最初は極小サイズにしておく
        if (defaultPlantPrefab != null && plantSpawnPoint != null)
        {
            currentPlantVisual = Instantiate(defaultPlantPrefab, plantSpawnPoint.position, Quaternion.identity, plantSpawnPoint);
            currentPlantVisual.transform.localScale = Vector3.one * 0.1f;
        }

        Debug.Log($"🌱 【{seedData.itemName}】を植えました！");
    }

    // 収穫するメソッド（UIから呼ばれる）
    public ItemData Harvest()
    {
        if (!isReadyToHarvest) return null;

        ItemData harvestedItem = plantedSeedData; // ※本来はここで「成長後のアイテム」に変換する処理を入れます

        // 初期化（土を空っぽにする）
        isPlanted = false;
        isReadyToHarvest = false;
        plantedSeedData = null;
        if (currentPlantVisual != null) Destroy(currentPlantVisual);

        return harvestedItem;
    }
}