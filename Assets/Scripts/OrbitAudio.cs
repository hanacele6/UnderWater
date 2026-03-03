using UnityEngine;
using System.Collections;

public class DeepSeaOrbitAudio : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform submarine; 
    [SerializeField] private float radius = 40f;
    [SerializeField] private float orbitSpeed = 0.05f;

    [Header("Audio Variation")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] ambientClips; // 複数の鳴き声を登録
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("Cooldown Settings")]
    [SerializeField] private float minWaitTime = 10f; // 最短の間隔
    [SerializeField] private float maxWaitTime = 30f; // 最長の間隔

    private float _angle;
    private bool _isWaiting = false;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        // 最初の一回を開始
        StartCoroutine(AudioRoutine());
    }

    void Update()
    {
        // 常に潜水艦の周りをゆっくり旋回（音の位置だけは常に動かす）
        _angle += orbitSpeed * Time.deltaTime;
        Vector3 offset = new Vector3(Mathf.Cos(_angle), Mathf.Sin(_angle) * 0.3f, Mathf.Sin(_angle)) * radius;
        transform.position = submarine.position + offset;
    }

    private IEnumerator AudioRoutine()
    {
        while (true)
        {
            // クールタイムの待機
            float wait = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(wait);

            // 音の再生
            PlayRandomSound();

            // 再生が終わるまで待機（重なりを防止する場合）
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
    }

    private void PlayRandomSound()
    {
        if (ambientClips.Length == 0) return;

        // ランダムなクリップを選択
        AudioClip clip = ambientClips[Random.Range(0, ambientClips.Length)];
        
        // ピッチをわずかに変えて、同じ音でも印象を変える
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        
        audioSource.clip = clip;
        audioSource.Play();
    }
}