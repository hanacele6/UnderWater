using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// IPointerEnterHandler等は、マウスがUIの上に乗った/離れたのを検知するUnityの便利機能です
public class ExtractionItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI設定")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    
    private ItemData myItem;
    private ExtractionUIManager manager;

    public void Setup(ItemData item, ExtractionUIManager mgr)
    {
        myItem = item;
        manager = mgr;

        if (iconImage != null && item.itemIcon != null) 
        {
            iconImage.sprite = item.itemIcon;
        }
        
        if (nameText != null) 
        {
            nameText.text = item.itemName;
        }
    }

    // マウスが乗った時
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager != null) manager.ShowTooltip(myItem);
    }

    // マウスが離れた時
    public void OnPointerExit(PointerEventData eventData)
    {
        if (manager != null) manager.HideTooltip();
    }
}