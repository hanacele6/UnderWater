using UnityEngine;
using System.Collections.Generic;

public class MissionGuide : MonoBehaviour
{
    public static MissionGuide Instance;

    [Header("マーカーのプレハブ")]
    [Tooltip("さっき作った図形のプレハブを入れてください")]
    public GameObject markerPrefab;

    [Header("動きの設定")]
    public float heightOffset = 2.0f;  
    public float bobbingSpeed = 2.0f;  
    public float bobbingAmount = 0.2f; 

    [Header("非表示設定")]
    public float hideDistance = 3.0f;

    private Transform playerTransform;

    // 💡 「どのターゲット」に「どのマーカー」が付いているかをペアで記憶する辞書
    private Dictionary<Transform, GameObject> activeMarkers = new Dictionary<Transform, GameObject>();

    void Awake()
    {
        Instance = this;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    // 💡 GameManagerから「複数のターゲット」をまとめて受け取る
    public void SetTargets(List<Transform> targets)
    {
        ClearAllMarkers(); // 一旦古いマーカーを全消去

        if (targets == null || targets.Count == 0 || markerPrefab == null) return;

        foreach (var target in targets)
        {
            if (target != null)
            {
                // ターゲットの数だけマーカーを生成して、辞書に登録
                GameObject newMarker = Instantiate(markerPrefab, transform); // 整理のためこのオブジェクトの子にする
                activeMarkers.Add(target, newMarker);
            }
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null || activeMarkers.Count == 0) return;

        // 💡 削除待ちのリスト（foreach中に辞書をいじるとエラーになるため）
        List<Transform> deadTargets = new List<Transform>();

        foreach (var pair in activeMarkers)
        {
            Transform target = pair.Key;
            GameObject marker = pair.Value;

            // ★ターゲットが破壊（回収）されていたら、死んだリストに入れる
            if (target == null)
            {
                deadTargets.Add(target);
                Destroy(marker); // マーカーも消す
                continue;
            }

            // 1. フワフワ移動と回転
            float newY = target.position.y + heightOffset + (Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount);
            marker.transform.position = new Vector3(target.position.x, newY, target.position.z);
            
            if (Camera.main != null)
            {
                marker.transform.Rotate(0f, 45f * Time.deltaTime, 0f);
            }

            // 2. プレイヤーとの距離で表示/非表示を切り替え
            UpdateVisibility(target, marker);
        }

        // 💡 死んだターゲットを辞書から取り除く
        foreach (var dead in deadTargets)
        {
            activeMarkers.Remove(dead);
        }
    }

    private void UpdateVisibility(Transform target, GameObject marker)
    {
        Renderer meshRenderer = marker.GetComponentInChildren<Renderer>();
        InteractableHighlight highlight = marker.GetComponentInChildren<InteractableHighlight>();

        float distance = Vector3.Distance(playerTransform.position, marker.transform.position);

        if (distance <= hideDistance)
        {
            // 近づいたら消す
            if (meshRenderer != null) meshRenderer.enabled = false;
            if (highlight != null) highlight.enabled = false;
        }
        else
        {
            // 離れたら表示して光らせる
            if (meshRenderer != null) meshRenderer.enabled = true;
            if (highlight != null)
            {
                highlight.OutlineMode = InteractableHighlight.Mode.OutlineAll;
                highlight.enabled = true;
            }
        }
    }

    public void ClearAllMarkers()
    {
        foreach (var marker in activeMarkers.Values)
        {
            if (marker != null) Destroy(marker);
        }
        activeMarkers.Clear();
    }
}