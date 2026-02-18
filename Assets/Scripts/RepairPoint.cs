using UnityEngine;
using UnityEngine.UI; // UIを使うため

public class RepairPoint : MonoBehaviour, IInteractable
{
    [Header("修復設定")]
    public bool isBroken = false;
    public float repairTime = 3f; // 直すのにかかる秒数
    public SubmarineStatus submarine; // 直した時にHPを少し回復させる用

    [Header("見た目の設定")]
    public GameObject brokenEffect; // 煙や火花のエフェクト（普段は消しておく）
    public GameObject progressBarUI; // 画面上の進捗バーUIの親
    public Image fillImage; // 進捗バーの中身（Image Typeを「Filled」にしたもの）

    private bool isRepairing = false;
    private float currentRepairTimer = 0f;

    void Start()
    {
        // 最初は壊れていない状態にしておく
        SetBrokenState(false);
    }

    public void SetBrokenState(bool broken)
    {
        isBroken = broken;
        isRepairing = false;
        currentRepairTimer = 0f;

        // エフェクトのON/OFF
        if (brokenEffect != null) brokenEffect.SetActive(broken);
        if (progressBarUI != null) progressBarUI.SetActive(false);
    }

    public string GetInteractPrompt()
    {
        if (!isBroken) return ""; // 壊れていなければ無視
        if (isRepairing) return "修復中...";
        return "修復を開始する";
    }

    public void Interact()
    {
        // 壊れていて、まだ修復していなければ、修復開始！
        if (isBroken && !isRepairing)
        {
            isRepairing = true;
            if (progressBarUI != null) progressBarUI.SetActive(true);
        }
    }

    void Update()
    {
        if (isRepairing)
        {
            // 時間を進める
            currentRepairTimer += Time.deltaTime;

            // バーの長さを更新（0.0 〜 1.0）
            if (fillImage != null) fillImage.fillAmount = currentRepairTimer / repairTime;

            // 修復完了！
            if (currentRepairTimer >= repairTime)
            {
                Debug.Log("修復が完了した！");
                SetBrokenState(false);
                
                // オマケ：直した時に潜水艦のHPを少し回復してあげる
                if (submarine != null) submarine.RepairHull(10f);
            }
        }
    }
}