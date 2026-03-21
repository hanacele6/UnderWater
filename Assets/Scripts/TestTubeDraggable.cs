using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TestTubeDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("試験管の設定")]
    public Image testTubeLiquidUI; // 試験管の中身の画像（Filled）
    public float maxLiquid = 100f;
    private float currentLiquid;

    [Header("注ぐギミックの設定")]
    [Tooltip("1秒間に注ぐ量")]
    public float pourRatePerSecond = 30f;
    [Tooltip("フラスコの口にどれくらい近づけたら注ぎ始めるか")]
    public float pourDistance = 150f;
    [Tooltip("注いでいる時の試験管の傾き（度）")]
    public float tiltAngle = 45f; // 右に傾ける場合は -45 など調整してください

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Quaternion originalRotation;
    
    private bool isDragging = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        originalRotation = rectTransform.rotation;
        
        currentLiquid = maxLiquid;
        if (testTubeLiquidUI != null) testTubeLiquidUI.fillAmount = 1f;
    }

    // ドラッグ開始
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        // 持っている間は、他のUIより手前に表示させる
        transform.SetAsLastSibling(); 
    }

    // ドラッグ中（マウスについていく）
    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    // ドラッグ終了（指を離した時）
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // 傾きと位置を元に戻す（試験管立てにカチャッと戻るイメージ）
        rectTransform.rotation = originalRotation;
        rectTransform.anchoredPosition = originalPosition;
    }

    private void Update()
    {
        // 持っている（ドラッグしている）間だけ、フラスコとの距離を監視する
        if (isDragging && currentLiquid > 0 && FlaskReceiver.Instance != null && !FlaskReceiver.Instance.IsFull)
        {
            // 試験管の先端と、フラスコの口の距離を測る
            float distance = Vector2.Distance(rectTransform.position, FlaskReceiver.Instance.openingPoint.position);

            if (distance <= pourDistance)
            {
                // ========== 注いでいる状態 ==========
                // 1. 試験管を傾ける
                rectTransform.rotation = Quaternion.Euler(0, 0, tiltAngle);

                // 2. 液体を移動させる
                float amountToPour = pourRatePerSecond * Time.deltaTime;
                
                // 試験管の残りが少ない場合は、残っている分だけにする
                if (currentLiquid < amountToPour) amountToPour = currentLiquid;

                currentLiquid -= amountToPour;
                if (testTubeLiquidUI != null) testTubeLiquidUI.fillAmount = currentLiquid / maxLiquid;

                // フラスコ側に「増えろ！」と命令を送る
                FlaskReceiver.Instance.ReceiveLiquid(amountToPour);
                
                // ※ここで「チョロチョロ…」という音を鳴らすと最高です
            }
            else
            {
                // ========== 近づいていない状態 ==========
                // 傾きを元に戻す
                rectTransform.rotation = originalRotation;
            }
        }
    }
}