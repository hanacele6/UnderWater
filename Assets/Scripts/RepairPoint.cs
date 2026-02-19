using UnityEngine;
using UnityEngine.UI; // 進捗バー(UI)を操作するために必要
using UnityEngine.InputSystem;

public enum RepairType
{
    Electrical, // 基盤バチバチ
    Wall        // 壁の破損・亀裂
}


public class RepairPoint : MonoBehaviour, IInteractable
{
    [Header("修復設定")]
    [Tooltip("この修復ポイントの種類を選んでください")]
    public RepairType repairType = RepairType.Electrical; 
    public bool isBroken = false;
    public float repairTime = 3f; 
    public SubmarineStatus submarine; 

    [Header("見た目の設定 (共通)")]
    public GameObject progressBarUI; 
    public Image fillImage; 

    [Header("見た目の設定 (基盤・エフェクト用)")]
    [Tooltip("壊れた時に出す火花や水漏れのエフェクト")]
    public GameObject brokenEffect; 

    [Header("見た目の設定 (壁の破損用)")]
    [Tooltip("正常な時の壁のモデル")]
    public GameObject normalWallModel;
    [Tooltip("壊れた時の壁のモデル（穴や亀裂があるもの）")]
    public GameObject brokenWallModel;

    private bool isRepairing = false;
    private float currentRepairTimer = 0f;
    [Header("プレイヤー設定")]
    [Tooltip("視点と移動を止めるためにPlayerInputをセットしてください")]
    public PlayerInput playerInput;

    private InteractableHighlight highlightScript;

    void Awake()
    {
        // 開始時に同じオブジェクトに付いている InteractableHighlight を取得
        highlightScript = GetComponent<InteractableHighlight>();
    }

    void Start()
    {
        SetBrokenState(false);
    }

    public void SetBrokenState(bool broken)
    {
        isBroken = broken;
        isRepairing = false;
        currentRepairTimer = 0f;

        if (progressBarUI != null) progressBarUI.SetActive(false);

        if (highlightScript != null)
        {
            highlightScript.isHighlightable = broken;
            
            // 直った瞬間に、もしプレイヤーが見つめていて光っていたら強制的に消灯させる
            //if (!broken) highlightScript.ToggleHighlight(false);
        }

        // ★修正：タイプに応じた見た目の切り替え
        if (repairType == RepairType.Electrical)
        {
            // 基盤の場合はエフェクトのON/OFFのみ
            if (brokenEffect != null) brokenEffect.SetActive(broken);
        }
        else if (repairType == RepairType.Wall)
        {
            // 壁の場合は、正常なモデルと壊れたモデルを「すり替える」
            if (normalWallModel != null) normalWallModel.SetActive(!broken);
            if (brokenWallModel != null) brokenWallModel.SetActive(broken);
            
            // ※壁が壊れた時に「水しぶき」などを出したい場合は、brokenEffectも同時に使えます
            if (brokenEffect != null) brokenEffect.SetActive(broken);
        }
    }

    public string GetInteractPrompt()
    {
        if (!isBroken) return ""; 
        if (isRepairing) return "修復中... (Qキーで中断)";
        
        // 種類に合わせて表示するテキストも変えるとオシャレです！
        return repairType == RepairType.Electrical ? "配電盤を修理する" : "壁の亀裂を溶接する";
    }

    public void Interact()
    {
        if (isBroken && !isRepairing)
        {
            isRepairing = true;
            if (progressBarUI != null) progressBarUI.SetActive(true);

            // 修理を開始したら、プレイヤーの操作（視点・移動）を完全にロック！
            if (playerInput != null) playerInput.enabled = false;
        }
    }

    void Update()
    {
        if (isRepairing)
        {
            // 途中でQキーを押したら修理を中断して逃げられるようにする
            if (Input.GetKeyDown(KeyCode.Q))
            {
                CancelRepair();
                return; // ここでUpdateの処理を抜ける
            }

            currentRepairTimer += Time.deltaTime;

            if (fillImage != null) 
            {
                fillImage.fillAmount = currentRepairTimer / repairTime;
            }

            if (currentRepairTimer >= repairTime)
            {
                SetBrokenState(false);
                if (submarine != null) submarine.RepairHull(25f);
                
                // 修理が完了したら、操作ロックを解除！
                if (playerInput != null) playerInput.enabled = true;
                
                Debug.Log("修理完了！");
            }
        }
    }

    // 修理を中断（キャンセル）した時の処理
    private void CancelRepair()
    {
        isRepairing = false;
        currentRepairTimer = 0f; // 進捗をリセットする（途中から再開したい場合はこの行を消す）
        
        if (progressBarUI != null) progressBarUI.SetActive(false);
        
        // 操作ロックを解除して逃げられるようにする！
        if (playerInput != null) playerInput.enabled = true;
        
        Debug.Log("修理を中断した！");
    }
}