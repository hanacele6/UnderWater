using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Centrifuge : MonoBehaviour
{
    [Header("遠心分離機の設定")]
    public Transform rotor; 
    public float spinDuration = 3.0f;
    public float maxSpinSpeed = 1500f;

    [Header("連携するピペットたち")]
    public List<PipetteReceiver> pipettes = new List<PipetteReceiver>();

    private bool isSpinning = false;

    // UIの「スタート」ボタンから呼ばれる
    public void StartSpin()
    {
        if (isSpinning) return;

        bool hasLiquid = false;
        foreach (var p in pipettes)
        {
            if (p != null && p.currentLiquid > 0f) hasLiquid = true;
        }

        if (hasLiquid)
        {
            StartCoroutine(SpinRoutine());
        }
        else
        {
            Debug.LogWarning("ピペットが空です！");
        }
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        Debug.Log("🌀 遠心分離スタート！");
        
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            
            // 徐々に速くなり、徐々に遅くなる滑らかな回転カーブ
            float speedMultiplier = Mathf.Sin((elapsed / spinDuration) * Mathf.PI);
            float currentSpeed = maxSpinSpeed * speedMultiplier;

            if (rotor != null)
            {
                rotor.Rotate(0, currentSpeed * Time.deltaTime, 0); // 回転軸はモデルに合わせて(X, Y, Z)変更してください
            }

            yield return null;
        }

        CompleteSpin();
    }

    private void CompleteSpin()
    {
        isSpinning = false;
        List<ItemData> results = new List<ItemData>();

        foreach (var p in pipettes)
        {
            if (p.currentLiquid > 0f && p.receivedPotion != null && p.receivedPotion.finalSample != null)
            {
                Debug.Log($"抽出直前チェック: アセット名={p.receivedPotion.finalSample.name}, アイテム名={p.receivedPotion.finalSample.itemName}, アイコン={p.receivedPotion.finalSample.itemIcon}");
                results.Add(p.receivedPotion.finalSample);
                p.EmptyPipette();
            }
        }

        BioReactorConsole console = FindObjectOfType<BioReactorConsole>();
        if (console != null) console.ShowResults(results);
    }
}