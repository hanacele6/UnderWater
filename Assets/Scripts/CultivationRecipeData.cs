using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCultivationRecipe", menuName = "Alchemy/CultivationRecipe")]
public class CultivationRecipeData : ScriptableObject
{
    [Header("培養設定")]
    public string recipeName;

    [Header("植えるために必要な細胞（種）")]
    [Tooltip("インベントリから消費するアイテム")]
    public SampleItemData requiredSeed;

    [Header("成長プロセス")]
    public int daysToGrow = 3;
    [Tooltip("成長段階ごとの3Dモデル（芽、茎、完成体など）")]
    public List<GameObject> growthPrefabs = new List<GameObject>();
    
    [Header("成長の大きさ設定")]
    [Tooltip("植えた直後の見た目の倍率（例：0.5なら元の半分のサイズ）")]
    public float startScaleMultiplier = 0.5f;

    [Tooltip("収穫時の見た目の倍率（例：1.0ならプレハブそのままのサイズ）")]
    public float endScaleMultiplier = 1.0f;

    [Header("収穫できる完成品")]
    public SampleItemData finalSample; 
}