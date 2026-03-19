using UnityEngine;
using UnityEngine.UI;

public class CargoPhysicsUI : MonoBehaviour
{
    public static CargoPhysicsUI Instance;

    [Header("UI設定")]
    [Tooltip("コンテナが落ちてくる親枠（パネル）")]
    public RectTransform containerDropArea;
    [Tooltip("コンテナの落下口（排出口）となる位置")]
    public RectTransform dropSpawnPoint;

    [Header("コンテナプレハブ設定")]
    public GameObject woodContainerPrefab;
    public GameObject ironContainerPrefab;
    public GameObject titaniumContainerPrefab;
    public GameObject missionContainerPrefab;

    void Awake() { Instance = this; }

    // 潜水艦がアイテムを拾った時にどこからでも呼べるメソッド
    public void DropContainer(CrateType type)
    {
        if (containerDropArea == null || dropSpawnPoint == null) return;

        GameObject prefabToDrop = null;

        // 種類に応じてプレハブを切り替え
        switch(type)
        {
            case CrateType.Wood: prefabToDrop = woodContainerPrefab; break;
            case CrateType.Iron: prefabToDrop = ironContainerPrefab; break;
            case CrateType.Titanium: prefabToDrop = titaniumContainerPrefab; break;
            case CrateType.Mission: prefabToDrop = missionContainerPrefab; break;
        }

        if (prefabToDrop == null) return;

        GameObject newContainer = Instantiate(prefabToDrop, containerDropArea);

        newContainer.transform.position = dropSpawnPoint.position;
        float randomOffset = Random.Range(-45f, 45f);
        newContainer.transform.localPosition += new Vector3(randomOffset, 0, 0);
        newContainer.transform.localEulerAngles = new Vector3(0, 0, Random.Range(-10f, 10f));
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