using UnityEngine;
using UnityEngine.EventSystems;

public class StirringTool : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [Header("設定")]
    [Tooltip("かき混ぜる中心点（ビーカーの中心にあるUIオブジェクトなどを指定）")]
    public RectTransform cauldronCenter; 
    
    [Tooltip("1周（360度）回した時に、何%進行するか")]
    public float progressPerFullCircle = 20f; 

    private float lastAngle = 0f;

    // マウスをクリックし始めた瞬間の角度を記憶
    public void OnPointerDown(PointerEventData eventData)
    {
        lastAngle = GetMouseAngle(eventData.position);
    }

    // クリックしたままマウスを動かしている間、ずっと呼ばれる
    public void OnDrag(PointerEventData eventData)
    {
        float currentAngle = GetMouseAngle(eventData.position);

        // 前回フレームからの角度の変化量（Delta）を計算
        float deltaAngle = Mathf.DeltaAngle(lastAngle, currentAngle);

        // 逆回転（マイナス）でも混ぜられるように絶対値（Abs）にする
        float absoluteMovement = Mathf.Abs(deltaAngle);

        // 動かした角度を、進行度（％）に変換して鍋に送る
        // 360度で指定した%分進むように計算
        float progressToAdd = (absoluteMovement / 360f) * progressPerFullCircle;
        
        if (progressToAdd > 0)
        {
           FlaskReceiver.Instance.AddMixProgress(progressToAdd);
        }

        lastAngle = currentAngle;
    }

    // 鍋の中心から見たマウスの「角度」を計算する数式
    private float GetMouseAngle(Vector2 mousePos)
    {
        // UI（Canvas）上の座標変換
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cauldronCenter.parent.GetComponent<RectTransform>(), 
            mousePos, 
            null, 
            out localMousePos
        );

        Vector2 direction = localMousePos - cauldronCenter.anchoredPosition;
        
        // Atan2は、XとYの距離から角度（ラジアン）を割り出す魔法の数式
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return angle;
    }
}