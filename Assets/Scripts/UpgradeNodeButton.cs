using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // 💡 マウスのホバーを検知するための追加

// IPointerEnterHandler, IPointerExitHandler を追加してホバーに対応させる
public class UpgradeNodeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI要素")]
    public TextMeshProUGUI nameText;
    public Image bgImage;
    
    private string myUpgradeID; 
    private UpgradeTreeUI myParentUI; // 親スクリプトを記憶しておく

    public void Setup(MainConsole.TechTreeNode nodeData, UpgradeTreeUI parentUI)
    {
        myUpgradeID = nodeData.upgradeID;
        myParentUI = parentUI;

        if (nameText != null) nameText.text = nodeData.displayName;
        
        if (bgImage != null)
        {
            bgImage.color = nodeData.isUnlocked ? new Color(0.5f, 1f, 0.5f) : new Color(0.3f, 0.3f, 0.3f);
        }

        Button btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        // 💡 クリックされたら「直接アンロック」を試みるように変更！
        btn.onClick.AddListener(() => myParentUI.TryUnlock(myUpgradeID));
    }

    // 💡 マウスカーソルが重なった時
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myParentUI != null) myParentUI.ShowDetail(myUpgradeID);
    }

    // 💡 マウスカーソルが離れた時
    public void OnPointerExit(PointerEventData eventData)
    {
        if (myParentUI != null) myParentUI.HideDetail();
    }
}