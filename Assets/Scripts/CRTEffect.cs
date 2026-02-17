using UnityEngine;
using UnityEngine.UI;

// ブラウン管の走査線（黒い帯）が上から下へ流れるロールエフェクト
public class CRTEffect : MonoBehaviour
{
    [Tooltip("波が下に流れるスピード")]
    public float scrollSpeed = 100f; // 少しゆっくりにしました
    [Tooltip("ソナー画面の縦幅")]
    public float screenHeight = 600f;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // シンプルに下へ移動させるだけ
        float newY = rectTransform.anchoredPosition.y - (scrollSpeed * Time.deltaTime);

        // 画面の下まで行ったら、画面の上に戻す（ループ）
        // ※少し余裕を持って画面外から戻すように調整
        if (newY < -screenHeight - 100f) 
        {
            newY = screenHeight + 100f;
        }

        // 位置を適用（X座標は0で固定）
        rectTransform.anchoredPosition = new Vector2(0f, newY);
    }
}