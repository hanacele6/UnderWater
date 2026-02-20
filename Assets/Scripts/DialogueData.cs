using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Adventure/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public struct Sentence
    {
        public string speakerName; // 話し手の名前
        [TextArea(3, 10)]
        public string text;        // セリフ本文
    }

    public List<Sentence> sentences = new List<Sentence>(); // セリフのリスト
}