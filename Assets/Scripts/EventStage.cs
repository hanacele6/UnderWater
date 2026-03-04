using UnityEngine;

// シーン内に配置し、イベント発生時の「立ち位置」や「NPCの配置」を管理する
public class EventStage : MonoBehaviour
{
    [Header("ステージID（台本と一致させる）")]
    public string stageID;

    [Header("役者の配置")]
    public GameObject npcPrefab;      // 出現させるNPC
    public Transform npcSpawnPoint;   // NPCの立つ位置
    private GameObject spawnedNPC;

    [Header("プレイヤーの制御")]
    public Transform playerStandPoint; // プレイヤーをワープさせる位置
    public Transform playerCamera;     // プレイヤーの視点カメラ
    public Transform lookTarget;       // 視点を固定する対象（NPCの顔など）

    // イベント開始時にGameManagerから呼ばれる
    public void SetupStage()
    {
        // 1. プレイヤーの移動と視点固定
        if (playerStandPoint != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            player.transform.position = playerStandPoint.position;
            
            // 視点をNPCに向ける
            if (playerCamera != null && lookTarget != null)
            {
                playerCamera.LookAt(lookTarget);
            }
        }

        // 2. NPCの生成
        if (npcPrefab != null && npcSpawnPoint != null)
        {
            spawnedNPC = Instantiate(npcPrefab, npcSpawnPoint.position, npcSpawnPoint.rotation);
        }
    }

    // イベント終了時にGameManagerから呼ばれる
    public void CleanupStage()
    {
        // 用が済んだNPCを消す（残したい場合は消さなくてもOK）
        if (spawnedNPC != null) Destroy(spawnedNPC);
    }
}