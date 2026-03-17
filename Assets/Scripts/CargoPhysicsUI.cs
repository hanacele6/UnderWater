using UnityEngine;
using UnityEngine.UI;

public class CargoPhysicsUI : MonoBehaviour
{
    public static CargoPhysicsUI Instance;

    [Header("UI設定")]
    [Tooltip("コンテナが落ちてくる親枠（パネル）")]
    public RectTransform containerDropArea;
    [Tooltip("通常のランダムガチャ用コンテナのプレハブ")]
    public GameObject normalContainerPrefab;
    [Tooltip("ミッション用の目立つコンテナのプレハブ")]
    public GameObject missionContainerPrefab;

    void Awake() { Instance = this; }

    // 潜水艦がアイテムを拾った時にどこからでも呼べるメソッド
    public void DropContainer(bool isMissionItem = false)
    {
        if (containerDropArea == null) return;

        GameObject prefabToDrop = isMissionItem ? missionContainerPrefab : normalContainerPrefab;
        if (prefabToDrop == null) return;

        // UI枠の上部、少しランダムなX座標から生成
        float randomX = Random.Range(-containerDropArea.rect.width / 3f, containerDropArea.rect.width / 3f);
        Vector3 spawnPos = new Vector3(randomX, containerDropArea.rect.height / 2f, 0);

        GameObject newContainer = Instantiate(prefabToDrop, containerDropArea);
        newContainer.transform.localPosition = spawnPos;

        // 生成時に少し回転をつけておくと、落ちた時にゴロゴロして可愛いです
        newContainer.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));
    }

    // ソナーを離れる時に箱を空っぽにする（船内へ移送）
    public void ClearContainers()
    {
        foreach (Transform child in containerDropArea)
        {
            Destroy(child.gameObject);
        }
    }
}