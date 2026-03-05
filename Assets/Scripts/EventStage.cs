using UnityEngine;
using System.Collections.Generic;

public class EventStage : MonoBehaviour
{
    [Header("ステージID（台本と一致させる）")]
    public string stageID;

    [System.Serializable]
    public struct ActorSetup
    {
        [Tooltip("台本の Speaker Name と完全に一致させてください")]
        public string actorName;
        
        [Tooltip("シーン内に本人がいない時だけ、このプレハブを新規生成します")]
        public GameObject npcPrefab;
        public Transform spawnPoint;
    }

    [Header("役者の配置（複数人登録OK！）")]
    public List<ActorSetup> actors = new List<ActorSetup>();
    
    // イベント管理用データ
    private List<GameObject> spawnedNPCs = new List<GameObject>(); // 今回新規で作ったNPCリスト
    private Dictionary<string, Transform> actorLookTargets = new Dictionary<string, Transform>();
    
    // ★追加：元からいたNPCの「帰る場所」を記憶しておく辞書
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> originalRotations = new Dictionary<GameObject, Quaternion>();

    [Header("プレイヤーの制御")]
    public Transform playerStandPoint; 
    public Transform playerCamera;     

    public void SetupStage()
    {
        // 1. プレイヤーの移動
        if (playerStandPoint != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                // ① 移動スクリプト等の干渉を防ぐため一瞬オフ
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                // ② プレイヤー本体をワープ
                player.transform.position = playerStandPoint.position;
                player.transform.rotation = playerStandPoint.rotation;

                Physics.SyncTransforms();

                // ③ 【追加】メインカメラも強制的にプレイヤーと同じ場所に持ってくる！
                if (Camera.main != null)
                {
                    Camera.main.transform.position = playerStandPoint.position;
                    // ※もしカメラが床にめり込む場合は、頭の高さ（Vector3.up * 1.5f など）を足してください
                }

                if (cc != null) cc.enabled = true;
            }
        }

        // 2. 登録された役者を配置する
        // シーン内にいるすべてのNPCを一旦リストアップする
        NPCController[] allNPCsInScene = FindObjectsOfType<NPCController>();

        foreach (var actor in actors)
        {
            GameObject targetNPC = null;
            bool isNewlySpawned = false;

            // ① シーン内にすでに「同じ名前のNPC」がいるか探す
            foreach (var npc in allNPCsInScene)
            {
                if (npc.characterName == actor.actorName)
                {
                    targetNPC = npc.gameObject;
                    break; // 見つけた！
                }
            }

            // ② 見つからなかった場合だけ、プレハブから新規生成する
            if (targetNPC == null && actor.npcPrefab != null && actor.spawnPoint != null)
            {
                targetNPC = Instantiate(actor.npcPrefab, actor.spawnPoint.position, actor.spawnPoint.rotation);
                spawnedNPCs.Add(targetNPC);
                isNewlySpawned = true;
            }

            // ③ ターゲットNPCを立ち位置にワープさせる
            // ③ ターゲットNPCを立ち位置にワープさせる
            if (targetNPC != null && actor.spawnPoint != null)
            {
                if (!isNewlySpawned)
                {
                    // 元からいたNPCを強制ワープさせる場合、イベント後に帰すために元の位置を記憶！
                    originalPositions[targetNPC] = targetNPC.transform.position;
                    originalRotations[targetNPC] = targetNPC.transform.rotation;
                }

                // ==========================================
                // NPCの物理エンジンやAIを完全に眠らせる
                // ==========================================
                UnityEngine.AI.NavMeshAgent agent = targetNPC.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                
                CharacterController cc = targetNPC.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                
                Rigidbody rb = targetNPC.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                // イベント指定の位置・向きへ強制移動（SetPositionAndRotationを使うとより確実です）
                targetNPC.transform.SetPositionAndRotation(actor.spawnPoint.position, actor.spawnPoint.rotation);

                // もしプレイヤーの立ち位置が設定されていれば、強制的にそっちを向かせる
                if (playerStandPoint != null)
                {
                    Vector3 dirToPlayer = playerStandPoint.position - targetNPC.transform.position;
                    dirToPlayer.y = 0; // 高さは無視（水平に振り向く）
                    if (dirToPlayer != Vector3.zero)
                    {
                        targetNPC.transform.rotation = Quaternion.LookRotation(dirToPlayer);
                    }
                }
                else
                {
                    // プレイヤー立ち位置がない場合はSpawnPointの向きに合わせる
                    targetNPC.transform.rotation = actor.spawnPoint.rotation;
                }

                Physics.SyncTransforms();

                // 物理エンジンやAIを目覚めさせる
                if (rb != null) rb.isKinematic = false;
                if (cc != null) cc.enabled = true;
                if (agent != null) agent.enabled = true;
                // ==========================================

                // カメラが向くターゲット（顔）を探して記憶
                Transform faceTransform = targetNPC.transform.Find("Head"); 
                if (faceTransform == null) faceTransform = targetNPC.transform; 
                actorLookTargets[actor.actorName] = faceTransform;
            }
        }
    }

    public void LookAtSpeaker(string speakerName)
    {
        if (playerCamera == null) return;
        if (actorLookTargets.ContainsKey(speakerName))
        {
            playerCamera.LookAt(actorLookTargets[speakerName]);
        }
    }

    public void CleanupStage()
    {
        // 新規生成したNPCは用済みなので消す
        foreach (var npc in spawnedNPCs)
        {
            if (npc != null) Destroy(npc);
        }
        spawnedNPCs.Clear();
        actorLookTargets.Clear();

        // ★追加：元からマップにいたNPCは、元の場所へワープさせて帰す
        foreach (var kvp in originalPositions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.position = kvp.Value;
                kvp.Key.transform.rotation = originalRotations[kvp.Key];
            }
        }
        originalPositions.Clear();
        originalRotations.Clear();
    }
}