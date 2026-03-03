using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AmbientSoundController : MonoBehaviour
{
    [SerializeField] private AudioClip[] ambientSounds;
    [SerializeField] private float minInterval = 10f;
    [SerializeField] private float maxInterval = 30f;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 2.0f; // フェードにかける時間
    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 0.5f;   // 環境音の最大音量

    private AudioSource audioSource;
    private float nextPlayTime;
    private bool isFading = false; // 二重再生防止

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0f; 
        audioSource.volume = 0f; // 初期音量は0
        SetNextPlayTime();
    }

    void Update()
    {
        // 再生中でなく、かつ待機時間を過ぎており、フェード中でもない場合に開始
        if (Time.time >= nextPlayTime && !audioSource.isPlaying && !isFading)
        {
            StartCoroutine(PlayWithFade());
        }
    }

    private IEnumerator PlayWithFade()
    {
        isFading = true;

        if (ambientSounds.Length > 0)
        {
            // クリップの選定
            int randomIndex = Random.Range(0, ambientSounds.Length);
            audioSource.clip = ambientSounds[randomIndex];
            audioSource.Play();

            // --- フェードイン ---
            float timer = 0;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(0f, maxVolume, timer / fadeDuration);
                yield return null;
            }
            audioSource.volume = maxVolume;

            // クリップが終わる直前まで待機
            // (クリップの長さ - フェードアウト時間) 分だけ待つ
            float waitTime = audioSource.clip.length - (fadeDuration * 2);
            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }

            // --- フェードアウト ---
            timer = 0;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(maxVolume, 0f, timer / fadeDuration);
                yield return null;
            }
            audioSource.volume = 0f;
            audioSource.Stop();
        }

        SetNextPlayTime();
        isFading = false;
    }

    private void SetNextPlayTime()
    {
        nextPlayTime = Time.time + Random.Range(minInterval, maxInterval);
    }
}