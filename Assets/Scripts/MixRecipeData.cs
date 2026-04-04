using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public struct TagRequirement
{
    public ItemTag requiredTag;
    public int requiredAmount; 
}

[CreateAssetMenu(fileName = "NewMixRecipe", menuName = "Alchemy/MixRecipe")]
public class MixRecipeData : ScriptableObject
{
    [Header("調合設定")]
    public string recipeName;
    public int priority = 0;
    public Color potionColor = Color.white;

    [Header("フラスコに入れる条件")]
    public List<TagRequirement> requiredTags = new List<TagRequirement>();
    public List<ItemTag> forbiddenTags = new List<ItemTag>();

    [Header("遠心分離で抽出される細胞（種）")]
    public SampleItemData finalSample;
}