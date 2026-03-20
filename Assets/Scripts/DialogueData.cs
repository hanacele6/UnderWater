using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Adventure/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    // ==========================================
    // 選択肢の構造（先に定義しておきます）
    // ==========================================
    [System.Serializable]
    public struct Choice
    {
        public string choiceText;            // 選択肢のボタンに表示する文字
        
        [Tooltip("選んだあとに飛ぶ先の『ラベル名』")]
        public string jumpToLabel;           
        
        [Tooltip("これを選んだらONにするフラグ（空欄なら何もしない）")]
        public string flagToSetOnChoose;     
    }

    // ==========================================
    // 1行のセリフの構造
    // ==========================================
    [System.Serializable]
    public struct Sentence
    {
        [Header("▼ 目印（ジャンプ先になる名前）")]
        [Tooltip("他の選択肢から飛んでくる時の目印。空欄でもOK")]
        public string label; 

        [Header("▼ セリフ内容")]
        public string speakerName;
        public Sprite portrait;
        
        [TextArea(3, 10)]
        public string text;

        [Header("▼ 全画面演出 (スチル画像 / 映像)")]
        public Sprite fullScreenImage; // このセリフで表示する全画面画像
        public UnityEngine.Video.VideoClip videoClip; // このセリフで再生する映像

        [Tooltip("ONにすると、このセリフになった瞬間に画像/動画を画面から消します")]
        public bool clearMedia; 

        [Tooltip("ONにすると、映像が終わるまでクリックしても次のセリフに進まなくなります")]
        public bool waitVideoFinish;
        
        [Header("演出設定")]
        [Range(0.5f, 2.0f)] public float voicePitch; // 1.0が標準

        [Header("▼ このセリフの後に選択肢を出す場合")]
        [Tooltip("ここに選択肢を追加すると、このセリフを読み終わった直後にボタンが出ます")]
        public List<Choice> choices;

        [Header("▼ 強制ジャンプ・終了設定")]
        [Tooltip("指定したラベルへ強制的に飛びます（分岐の合流用）。『END』と入力するとそこで会話が終了します。")]
        public string forceJumpLabel;
    }
    

    [Header("会話の台本")]
    public List<Sentence> sentences = new List<Sentence>(); // セリフのリスト

    [Header("フラグ設定（オプション）")]
    [Tooltip("この会話を読み終わった時に立てるフラグ（空欄なら何もしない）")]
    public string flagToSetOnComplete;

    [Header("システム設定")]
    [Tooltip("ONにすると、会話中もプレイヤーが動けたりソナーを操作できたりします")]
    public bool canInteractDuringDialogue = false;
}