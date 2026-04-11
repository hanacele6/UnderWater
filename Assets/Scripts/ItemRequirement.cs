using UnityEngine;

// 💡 MonoBehaviourを消して [System.Serializable] をつけるのがポイント！
[System.Serializable]
public class ItemRequirement
{
    [Tooltip("要求されるアイテム")]
    public ItemData item;   

    [Tooltip("必要な数")]
    public int amount = 1;  
}