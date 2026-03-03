using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Adventure/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public struct Sentence
    {
        public string speakerName;
        public Sprite portrait;
        [TextArea(3, 10)]
        public string text;
        
        [Header("演出設定")]
        [Range(0.5f, 2.0f)] public float voicePitch; // 追加：1.0が標準
    }

    public List<Sentence> sentences = new List<Sentence>(); // セリフのリスト

    [Header("フラグ設定（オプション）")]
    [Tooltip("この会話を読み終わった時に立てるフラグ（空欄なら何もしない）")]
    public string flagToSetOnComplete;
}