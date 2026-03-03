using UnityEngine;
using System.Collections;

public class BGMManager : MonoBehaviour
{
    [System.Serializable]
    public struct PhaseBGM
    {
        public string phaseName; // 例: "OPERATION", "STANDBY" など
        public AudioClip bgmClip;
    }

    [SerializeField] private PhaseBGM[] phaseBGMs;
    [SerializeField] private AudioSource[] audioSources = new AudioSource[2];
    [SerializeField] private float crossfadeDuration = 2.0f;

    private int activeSourceIndex = 0;

    // 既存のフラグシステムからこのメソッドを呼び出してフェーズを伝達します
    public void ChangePhase(string newPhase)
    {
        AudioClip nextClip = GetClipForPhase(newPhase);
        if (nextClip != null)
        {
            StartCoroutine(CrossfadeBGM(nextClip));
        }
    }

    private AudioClip GetClipForPhase(string phase)
    {
        foreach (var p in phaseBGMs)
        {
            if (p.phaseName == phase) return p.bgmClip;
        }
        return null;
    }

    private IEnumerator CrossfadeBGM(AudioClip nextClip)
    {
        int nextSourceIndex = 1 - activeSourceIndex;
        AudioSource activeSource = audioSources[activeSourceIndex];
        AudioSource nextSource = audioSources[nextSourceIndex];

        nextSource.clip = nextClip;
        nextSource.volume = 0f;
        nextSource.Play();

        float timer = 0f;
        while (timer < crossfadeDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / crossfadeDuration;
            
            activeSource.volume = Mathf.Lerp(1f, 0f, normalizedTime);
            nextSource.volume = Mathf.Lerp(0f, 1f, normalizedTime);
            yield return null;
        }

        activeSource.Stop();
        activeSourceIndex = nextSourceIndex;
    }
}