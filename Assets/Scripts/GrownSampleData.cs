using UnityEngine;
using System.Collections.Generic;

// レシピの条件を定義するための構造体
[System.Serializable]
public struct TagRequirement
{
    public ItemTag requiredTag;
    [Tooltip("このタグの合計ポイントがいくつ必要か")]
    public int requiredAmount; 
}

[CreateAssetMenu(fileName = "NewGrownSample", menuName = "Inventory/GrownSampleData")]
public class GrownSampleData : ScriptableObject
{
    [Header("完成品データ")]
    public string sampleName;
    [TextArea(3, 5)]
    public string description;
    public Sprite completedIcon;
    public GameObject samplePrefab; // 水槽の中で育っていく様子や完成品の3Dモデル

    [Header("培養設定")]
    [Tooltip("完成までに必要な日数")]
    public int daysToGrow = 3;


    [Header("レシピ条件")]
    [Tooltip("例：Toxicが2以上、Meatが1以上 など")]
    public List<TagRequirement> requiredTags = new List<TagRequirement>();

    [Tooltip("このタグが入っていたら、この生物にはならない（失敗や変異の条件に使う）")]
    public List<ItemTag> forbiddenTags = new List<ItemTag>();

    [Tooltip("条件が被った場合、数字が大きい方が優先して完成する")]
    public int priority = 0;
}