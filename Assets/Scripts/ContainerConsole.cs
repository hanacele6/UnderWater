using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContainerConsole : MonoBehaviour, IInteractable
{
    [Header("連携設定")]
    public SubmarineStatus submarine;
    
    [Header("コンテナギミック設定")]
    public Animator containerAnimator; 
    public float animationDuration = 1.0f; 

    [Header("ランダムアイテムのプール")]
    public ItemData[] randomItemPool;

    private bool isOperating = false;

    public string GetInteractPrompt()
    {
        if (isOperating) return "処理中...";
        if (submarine.cargoQueue.Count > 0) 
            return $"コンテナを引き上げる (回収物: {submarine.cargoQueue.Count}個)";

        return "回収されたアイテムはありません";
    }

    public void Interact()
    {
        if (isOperating || submarine.cargoQueue.Count == 0) return;
        StartCoroutine(ExtractUIProcessRoutine());
    }

    private IEnumerator ExtractUIProcessRoutine()
    {
        isOperating = true;

        // 1. コンテナを開くアニメーション
        if (containerAnimator != null) containerAnimator.SetTrigger("OpenTrigger");
        yield return new WaitForSeconds(animationDuration);

        // 2. カーゴの中身を「リスト」として取り出す
        List<ItemData> extractedItems = new List<ItemData>();

        while (submarine.cargoQueue.Count > 0)
        {
            ItemData extractedData = submarine.cargoQueue.Dequeue();

            // ランダム補充ロジック
            if (extractedData == null && randomItemPool.Length > 0)
            {
                int randomIndex = Random.Range(0, randomItemPool.Length);
                extractedData = randomItemPool[randomIndex];
            }

            if (extractedData != null)
            {
                extractedItems.Add(extractedData);
            }
        }

        // ==========================================
        // 3. UIを開き、プレイヤーが「回収ボタン」を押して閉じるまで待機する
        // ==========================================
        bool isUIClosed = false;

        if (ExtractionUIManager.Instance != null)
        {
            // UI側にリストを渡し、「閉じられたら isUIClosed を true にしてね」と約束（コールバック）を渡す
            ExtractionUIManager.Instance.OpenExtractionUI(extractedItems, () => { isUIClosed = true; });
        }
        else
        {
            Debug.LogError("ExtractionUIManagerが見つかりません！");
            isUIClosed = true; 
        }

        // UIが閉じられるまでここで無限に待つ（進行不能バグの心配なし！）
        yield return new WaitUntil(() => isUIClosed);

        // 4. コンテナを閉じるアニメーション
        if (containerAnimator != null) containerAnimator.SetTrigger("CloseTrigger");
        yield return new WaitForSeconds(animationDuration);

        isOperating = false; 
    }
}