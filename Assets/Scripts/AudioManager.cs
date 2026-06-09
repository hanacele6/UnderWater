using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("スピーカー (AudioSource)")]
    [Tooltip("ループ再生をONにしておく")]
    public AudioSource bgmSource;
    [Tooltip("ループ再生はOFF")]
    public AudioSource seSource;
    [Tooltip("ループ再生はOFF、Volumeは0にしておく")]
    public AudioSource ambientSource;

    [Header("環境音 (Ambient) 設定")]
    [SerializeField] private AudioClip[] ambientSounds;
    [SerializeField] private float minInterval = 10f;
    [SerializeField] private float maxInterval = 30f;
    [SerializeField] private float fadeDuration = 2.0f;
    [Range(0f, 1f)]
    [SerializeField] private float maxAmbientVolume = 0.5f;

    private float nextAmbientPlayTime;
    private bool isAmbientFading = false;
    
    // 💡 環境音を一時停止するためのフラグ
    public bool isAmbientActive = true; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (ambientSource != null)
        {
            ambientSource.spatialBlend = 0f; 
            ambientSource.volume = 0f;
            SetNextAmbientPlayTime();
        }
    }

    private void Update()
    {
        // 環境音がONで、再生中でなく、待機時間を過ぎており、フェード中でもない場合に開始
        if (isAmbientActive && ambientSource != null && ambientSounds.Length > 0)
        {
            if (Time.time >= nextAmbientPlayTime && !ambientSource.isPlaying && !isAmbientFading)
            {
                StartCoroutine(PlayAmbientWithFade());
            }
        }
    }

    // ==========================================
    // BGM・SE 再生メソッド
    // ==========================================
    
    // 💡 BGMを切り替える（同じ曲ならそのまま流す）
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // 💡 効果音を1回だけ鳴らす
    public void PlaySE(AudioClip clip)
    {
        if (clip != null) seSource.PlayOneShot(clip);
    }

    // ==========================================
    // 環境音 (Ambient) 自動再生ロジック
    // ==========================================

    private IEnumerator PlayAmbientWithFade()
    {
        isAmbientFading = true;

        int randomIndex = Random.Range(0, ambientSounds.Length);
        ambientSource.clip = ambientSounds[randomIndex];
        ambientSource.Play();

        // --- フェードイン ---
        float timer = 0;
        while (timer < fadeDuration)
        {
            if (!isAmbientActive) break; // 途中で強制ストップされた時用
            
            timer += Time.deltaTime;
            ambientSource.volume = Mathf.Lerp(0f, maxAmbientVolume, timer / fadeDuration);
            yield return null;
        }
        
        if (isAmbientActive) ambientSource.volume = maxAmbientVolume;

        // クリップが終わる直前まで待機
        float waitTime = ambientSource.clip.length - (fadeDuration * 2);
        if (waitTime > 0)
        {
            // 待機中もストップフラグを監視する
            float waited = 0;
            while (waited < waitTime && isAmbientActive)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        // --- フェードアウト ---
        timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // 急なストップ時も綺麗にフェードアウトさせる
            ambientSource.volume = Mathf.Lerp(ambientSource.volume, 0f, timer / fadeDuration);
            yield return null;
        }
        
        ambientSource.volume = 0f;
        ambientSource.Stop();

        SetNextAmbientPlayTime();
        isAmbientFading = false;
    }

    private void SetNextAmbientPlayTime()
    {
        nextAmbientPlayTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    // 💡 外部から環境音のON/OFFを切り替える便利メソッド
    public void SetAmbientActive(bool active)
    {
        isAmbientActive = active;
        if (!active && ambientSource.isPlaying)
        {
            // 即座にフェードアウトして止める処理を呼ぶことも可能（今回はシンプルに次から鳴らない仕様）
        }
    }
}