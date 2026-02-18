using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContainerConsole : MonoBehaviour, IInteractable
{
    [Header("連携設定")]
    public SubmarineStatus submarine;
    
    [Header("コンテナギミック設定")]
    public Transform itemSpawnPoint;
    public GameObject basePickupPrefab;
    public Animator containerAnimator; 
    
    [Tooltip("アニメーションが完了するまでの待機時間（秒）")]
    public float animationDuration = 1.0f; 
    
    [Tooltip("アイテムが弾け飛ぶ勢い")]
    public float scatterForce = 3f; 

    [Tooltip("アイテム放出後、フタが閉まり始めるまでの待機時間")]
    public float closeDelay = 5.0f;

    [Header("ランダムアイテムのプール")]
    public ItemData[] randomItemPool;

    private bool isOperating = false; // 演出中に連続で押せないようにするロック

    public string GetInteractPrompt()
    {
        if (isOperating) return "処理中...";
        
        if (submarine.cargoQueue.Count > 0) 
            return $"コンテナを引き上げる (回収物: {submarine.cargoQueue.Count}個)";

        return "回収されたアイテムはありません";
    }

    public void Interact()
    {
        // 演出中、またはストックが無い場合は何もしない
        if (isOperating || submarine.cargoQueue.Count == 0) return;

        // コルーチン（時間差処理）をスタート！
        StartCoroutine(ExtractAllItemsRoutine());
    }

    private IEnumerator ExtractAllItemsRoutine()
    {
        isOperating = true;

        // 1. アニメーションを再生（開く）
        if (containerAnimator != null)
        {
            containerAnimator.SetTrigger("OpenTrigger");
        }

        // 2. 開ききるまで待つ
        yield return new WaitForSeconds(animationDuration);

        List<GameObject> spawnedItems = new List<GameObject>();

        // 3. アイテムを全部出す
        while (submarine.cargoQueue.Count > 0)
        {
            ItemData extractedData = submarine.cargoQueue.Dequeue();

            if (extractedData == null && randomItemPool.Length > 0)
            {
                int randomIndex = Random.Range(0, randomItemPool.Length);
                extractedData = randomItemPool[randomIndex];
            }

            GameObject newItem = Instantiate(basePickupPrefab, itemSpawnPoint.position, Random.rotation);
            
            PickupItem pickupScript = newItem.GetComponent<PickupItem>();
            if (pickupScript != null) pickupScript.Initialize(extractedData);

            Rigidbody rb = newItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 scatterDir = Vector3.up + Random.insideUnitSphere * 0.5f;
                rb.AddForce(scatterDir.normalized * scatterForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * scatterForce, ForceMode.Impulse);
            }

            // ★追加：出したアイテムを監視リストに登録する
            spawnedItems.Add(newItem);
        }

        // ==========================================
        // すべてのアイテムが拾われるまで無限に待機する
        // ==========================================
        // Unityでは Destroy() されたオブジェクトは null になるので、
        // リストの中身が「すべて null」になるまでここで処理が一時停止します。
        yield return new WaitUntil(() => 
        {
            foreach (GameObject item in spawnedItems)
            {
                if (item != null) return false; // まだ拾われていないアイテムが残っている！
            }
            return true; // 全て null になった（全部拾い終わった）！
        });

        // 4. 全て拾い終えたのを確認したら、指定した秒数（closeDelay）だけ余韻を残して待つ
        yield return new WaitForSeconds(closeDelay);

        // 5. 「閉じる」トリガーを引く
        if (containerAnimator != null)
        {
            containerAnimator.SetTrigger("CloseTrigger");
        }
        
        // 6. 閉まりきるまで待つ
        yield return new WaitForSeconds(animationDuration);

        // 全ての処理が終わったので、コンソールのロックを解除する
        isOperating = false; 
    }
}