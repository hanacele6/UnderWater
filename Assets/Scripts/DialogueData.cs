using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Adventure/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public struct Sentence
    {
        public string speakerName; // 話し手の名前
        public Sprite portrait; //立ち絵
        [TextArea(3, 10)]
        public string text;        // セリフ本文
    }

    public List<Sentence> sentences = new List<Sentence>(); // セリフのリスト

    [Header("フラグ設定（オプション）")]
    [Tooltip("この会話を読み終わった時に立てるフラグ（空欄なら何もしない）")]
    public string flagToSetOnComplete;
}