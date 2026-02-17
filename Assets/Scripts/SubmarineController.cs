using UnityEngine;

public class SubmarineController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 5f; // 前進スピード
    public float turnSpeed = 30f; // 旋回スピード

    // 現在、プレイヤーが操縦中かどうかを判定するフラグ
    public bool isPiloting = false; 

    void Update()
    {
        // 操縦中じゃなければ、以下の操作処理は一切無視する！
        if (!isPiloting) return;

        // 操縦中のW/A/S/D操作（※Starter AssetsのPlayerInputをオフにしても動くように、直接キー入力を検知します）
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
        }
    }
}