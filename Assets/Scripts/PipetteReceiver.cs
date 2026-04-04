using UnityEngine;

public class PipetteReceiver : MonoBehaviour
{
    [Header("ピペットの設定")]
    public float maxLiquid = 20f; // フラスコ(100f)の5分の1のサイズ
    public float currentLiquid = 0f;

    [Header("3D設定")]
    public Transform openingPoint; // 上の口
    public Transform bottomPoint;  // 底（液面の計算用）
    public Renderer liquidRenderer;

    public bool IsFull => currentLiquid >= (maxLiquid - 0.5f);
    
    public MixRecipeData receivedPotion { get; private set; }

    private Material liquidMat;
    private const string FillLevelProp = "_FillLevel";

    void Start()
    {
        if (liquidRenderer != null) liquidMat = liquidRenderer.material;
        
        // 最初は空っぽにしておく
        if (liquidMat != null && bottomPoint != null)
        {
            liquidMat.SetFloat(FillLevelProp, bottomPoint.position.y - 1.0f);
        }
    }

    // フラスコから「毎フレーム」呼ばれて少しずつ溜まる
    public void ReceiveLiquid(float amount, MixRecipeData potionData)
    {
        if (IsFull) return;

        // 最初に注がれた瞬間に、色と完成品データを記憶する
        if (receivedPotion == null && potionData != null)
        {
            receivedPotion = potionData;
            if (liquidMat != null) 
            {
                liquidMat.SetColor("_BaseColor", potionData.potionColor);
                // Emission（発光）があるマテリアルならそれも変える
            }
        }

        // 液量を増やす
        currentLiquid += amount;

        // シェーダーの液面を上に持ち上げる
        if (liquidMat != null && bottomPoint != null && openingPoint != null)
        {
            float ratio = currentLiquid / maxLiquid;
            float currentY = Mathf.Lerp(bottomPoint.position.y, openingPoint.position.y, ratio);
            liquidMat.SetFloat(FillLevelProp, currentY);
        }

        // 満タンになった瞬間
        if (currentLiquid >= maxLiquid)
        {
            currentLiquid = maxLiquid;
            Debug.Log($"🧬 ピペットが【{receivedPotion.recipeName}】で満タンになりました！次のピペットへ！");
        }
    }

    public void EmptyPipette()
    {
        currentLiquid = 0f;
        receivedPotion = null;

        // シェーダーの液面を一番下（見えない位置）に下げる
        if (liquidMat != null && bottomPoint != null)
        {
            liquidMat.SetFloat(FillLevelProp, bottomPoint.position.y - 1.0f);
        }
    }
}