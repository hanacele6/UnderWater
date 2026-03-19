using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MissionAreaTrigger : MonoBehaviour
{
    public enum AreaType
    {
        ReachArea,      // ① 到達
        StayAndScan,    // ② 待機（環境スキャン）
        RetrieveItem    // ③ 回収（ミッション重要アイテム）
    }

    [Header("目的ギミックの設定")]
    public AreaType areaType = AreaType.ReachArea;
    public string flagToSet; 
    public string reachMessage = "目標を達成した";

    [Header("②待機 / ③回収 の設定")]
    public float requiredStayTime = 3f; 
    [Tooltip("回収ミッションの場合、コンテナUIに降らせるための見た目データなどを指定")]
    public ItemData missionItemData; // ※もしアイテムデータがあれば

    private float currentStayTime = 0f;
    private bool isCleared = false;

    void Start()
    {
        GetComponent<Collider>().isTrigger = true; 
        if (GetComponent<MeshRenderer>() != null) GetComponent<MeshRenderer>().enabled = false;
    }

    void OnTriggerStay(Collider other)
    {
        if (isCleared) return;

        SubmarineController sub = other.GetComponent<SubmarineController>();
        if (sub == null) sub = other.GetComponentInParent<SubmarineController>();

        if (sub != null || other.CompareTag("Player"))
        {
            switch (areaType)
            {
                case AreaType.ReachArea:
                    CompleteMission(sub);
                    break;
                case AreaType.StayAndScan:
                    currentStayTime += Time.deltaTime;
                    if (currentStayTime >= requiredStayTime) CompleteMission(sub);
                    break;
                case AreaType.RetrieveItem:
                    // ③ 回収の場合は、潜水艦の真上に少し留まって引き上げるような演出時間を設けても面白いです
                    currentStayTime += Time.deltaTime;
                    if (currentStayTime >= requiredStayTime) CompleteMission(sub);
                    break;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (areaType == AreaType.StayAndScan || areaType == AreaType.RetrieveItem)
            currentStayTime = 0f;
    }

    private void CompleteMission(SubmarineController sub)
    {
        isCleared = true;

        if (GameManager.Instance != null && !string.IsNullOrEmpty(flagToSet))
        {
            GameManager.Instance.SetFlag(flagToSet, true);
            GameManager.Instance.UpdateMainMissionHUD();
        }
        
        // ★ 回収ミッションなら、ここでUIの箱に「特別仕様のコンテナ」を物理で落とす処理を呼ぶ
        if (areaType == AreaType.RetrieveItem && CargoPhysicsUI.Instance != null)
        {
           CargoPhysicsUI.Instance.DropContainer(CrateType.Mission);
        }

        if (!string.IsNullOrEmpty(reachMessage) && UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage(reachMessage);
        }

        Destroy(gameObject);
    }
}