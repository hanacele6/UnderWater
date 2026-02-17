using UnityEngine;
using UnityEngine.UI;

public class EchoBlip : MonoBehaviour
{
    public float fadeTime = 3f;
    private Image img;
    private Color originalColor;

    // ★追加：SonarManagerから情報を受け取って形と色を変えるメソッド
    public void Setup(Color echoColor, float depthLength, float angle)
    {
        img = GetComponent<Image>();
        originalColor = echoColor;
        img.color = originalColor;

        RectTransform rt = GetComponent<RectTransform>();
        
        // 幅を少し持たせつつ、高さを「奥行き（面）」にする
        rt.sizeDelta = new Vector2(4f, depthLength);
        
        // 走査線と同じ角度に回転させることで、壁の厚みのように見せる
        rt.localEulerAngles = new Vector3(0, 0, angle);

        Destroy(gameObject, fadeTime); 
    }

    void Update()
    {
        // 従来通り、ゆっくり消えていく
        originalColor.a -= (1f / fadeTime) * Time.deltaTime;
        img.color = originalColor;
    }
}