using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UpgradeTreeUI : MonoBehaviour
{
    [Header("ノード生成設定")]
    public GameObject nodePrefab;
    public GameObject linePrefab; // 💡 追加：線用のプレハブ！
    public Transform treeViewport;

    [Header("詳細パネル")]
    public GameObject detailPanel;
    public TextMeshProUGUI upgradeNameText;
    public TextMeshProUGUI upgradeDescText;
    public TextMeshProUGUI requirementText;
    // ※ unlockButton の変数は削除しました！

    public void InitializeTree()
    {
        HideDetail();
        GenerateTreeNodes();

        RectTransform rt = treeViewport.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
        }
    }

    private void GenerateTreeNodes()
    {
        // 古いノードと線をすべて削除
        foreach (Transform child in treeViewport)
        {
            Destroy(child.gameObject);
        }

        if (MainConsole.Instance == null) return;

        // 【1】まず「表示すべきノード」だけをリストアップする
        List<MainConsole.TechTreeNode> visibleNodes = new List<MainConsole.TechTreeNode>();
        foreach (var node in MainConsole.Instance.upgradeTree)
        {
            bool isVisible = false;
            if (string.IsNullOrEmpty(node.requiredPreviousID)) isVisible = true;
            else if (node.isUnlocked) isVisible = true;
            else
            {
                var prevNode = MainConsole.Instance.upgradeTree.Find(n => n.upgradeID == node.requiredPreviousID);
                if (prevNode != null && prevNode.isUnlocked) isVisible = true;
            }

            if (isVisible) visibleNodes.Add(node);
        }

        // 💡【2】先に「線」を描画する（ノードより後ろ（下）に表示させるため）
        foreach (var node in visibleNodes)
        {
            if (!string.IsNullOrEmpty(node.requiredPreviousID))
            {
                var prevNode = visibleNodes.Find(n => n.upgradeID == node.requiredPreviousID);
                if (prevNode != null)
                {
                    // 双方の座標を繋ぐ線を引く！
                    DrawLine(prevNode.uiPosition, node.uiPosition, prevNode.isUnlocked && node.isUnlocked);
                }
            }
        }

        // 【3】その上に「ノード（ボタン）」を配置する
        foreach (var node in visibleNodes)
        {
            GameObject newObj = Instantiate(nodePrefab, treeViewport);
            RectTransform rt = newObj.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = node.uiPosition;

            UpgradeNodeButton nodeScript = newObj.GetComponent<UpgradeNodeButton>();
            if (nodeScript != null) nodeScript.Setup(node, this);
        }
    }

    // 💡 プログラムで動的にUIの「線」を引く魔法のメソッド
    private void DrawLine(Vector2 startPos, Vector2 endPos, bool bothUnlocked)
    {
        if (linePrefab == null) return;

        GameObject lineObj = Instantiate(linePrefab, treeViewport);
        RectTransform rt = lineObj.GetComponent<RectTransform>();

        // 距離と角度の計算
        Vector2 dir = endPos - startPos;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 中心位置、長さ、角度を設定
        rt.anchoredPosition = startPos + dir / 2f;
        rt.sizeDelta = new Vector2(distance, 5f); // 横幅を距離にし、縦幅（太さ）を 5f にする
        rt.localEulerAngles = new Vector3(0, 0, angle);

        // 解放済みなら明るく、未解放なら暗い色にする
        Image img = lineObj.GetComponent<Image>();
        if (img != null)
        {
            img.color = bothUnlocked ? new Color(0.5f, 1f, 0.5f) : new Color(0.3f, 0.3f, 0.3f);
        }
    }

    // ==========================================
    // マウスホバー ＆ クリック処理
    // ==========================================

    // 💡 マウスが乗った時に呼ばれる
    public void ShowDetail(string upgradeID)
    {
        var nodeData = MainConsole.Instance.upgradeTree.Find(n => n.upgradeID == upgradeID);
        if (nodeData == null) return;

        detailPanel.SetActive(true);
        upgradeNameText.text = nodeData.displayName;
        upgradeDescText.text = nodeData.description;

        if (nodeData.isUnlocked)
        {
            requirementText.text = "<color=green>【解放済み】</color>";
            return;
        }

        string reqString = "【必要条件（クリックで解放）】\n";
        bool canUnlock = true;

        if (!string.IsNullOrEmpty(nodeData.requiredPreviousID))
        {
            var prevNode = MainConsole.Instance.upgradeTree.Find(n => n.upgradeID == nodeData.requiredPreviousID);
            if (prevNode != null && !prevNode.isUnlocked)
            {
                reqString += $"<color=red>✗ 前提：{prevNode.displayName}</color>\n";
                canUnlock = false;
            }
        }

        foreach (var req in nodeData.requirements)
        {
            int currentCount = InventoryManager.Instance.GetItemCount(req.item);
            if (currentCount >= req.amount) reqString += $"<color=green>✓ {req.item.itemName} ({currentCount}/{req.amount})</color>\n";
            else { reqString += $"<color=red>✗ {req.item.itemName} ({currentCount}/{req.amount})</color>\n"; canUnlock = false; }
        }

        // 足りない場合はグレー文字にするなどの工夫
        if (!canUnlock) reqString += "\n<color=gray>素材または前提条件が不足しています</color>";
        
        requirementText.text = reqString;
    }

    // 💡 マウスが離れた時に呼ばれる
    public void HideDetail()
    {
        detailPanel.SetActive(false);
    }

    // 💡 クリックした時に呼ばれる（ボタンから直接呼ばれる）
    public void TryUnlock(string upgradeID)
    {
        if (MainConsole.Instance == null) return;
        
        // 成功すれば素材が消費され、ログが出る
        bool success = MainConsole.Instance.TryUnlockUpgrade(upgradeID);
        if (success)
        {
            // 成功したら画面をリフレッシュして、ホバーの詳細も最新にする
            GenerateTreeNodes();
            ShowDetail(upgradeID); 
        }
    }
}