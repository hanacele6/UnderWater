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

    [Header("収穫できる完成品")]
    public SampleItemData finalSample; 
}