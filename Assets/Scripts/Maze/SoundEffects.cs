using UnityEngine;

namespace MazeGame
{
    public class SoundEffects : MonoBehaviour
    {
        public static SoundEffects Instance { get; private set; }

        private AudioSource _audioSource;
        private AudioClip _coinClip;
        private AudioClip _winClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            _coinClip = CreateCoinSound();
            _winClip = CreateWinSound();
        }

        public void PlayCoinSound()
        {
            if (_audioSource != null && _coinClip != null)
            {
                _audioSource.pitch = Random.Range(0.95f, 1.15f);
                _audioSource.PlayOneShot(_coinClip, 0.7f);
            }
        }

        public void PlayWinSound()
        {
            if (_audioSource != null && _winClip != null)
            {
                _audioSource.pitch = 1.0f;
                _audioSource.PlayOneShot(_winClip, 0.9f);
            }
        }

        private AudioClip CreateCoinSound()
        {
            int sampleRate = 44100;
            float duration = 0.22f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // 2 ascending tones (B5 -> E6)
            float freq1 = 987.77f;
            float freq2 = 1318.51f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float currentFreq = (t < duration * 0.45f) ? freq1 : freq2;
                float envelope = 1f - (t / duration);
                // Sine wave with slight harmonic
                float wave = Mathf.Sin(2f * Mathf.PI * currentFreq * t) * 0.7f +
                             Mathf.Sin(4f * Mathf.PI * currentFreq * t) * 0.3f;
                samples[i] = wave * envelope * 0.6f;
            }

            AudioClip clip = AudioClip.Create("CoinPickup", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateWinSound()
        {
            int sampleRate = 44100;
            float duration = 0.8f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // 4 note fanfare: C5, E5, G5, C6
            float[] freqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDuration = duration / freqs.Length;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t / noteDuration), 0, freqs.Length - 1);
                float noteT = t - (noteIndex * noteDuration);
                float envelope = Mathf.Clamp01(1f - (noteT / noteDuration));

                float currentFreq = freqs[noteIndex];
                float wave = Mathf.Sin(2f * Mathf.PI * currentFreq * t);
                samples[i] = wave * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("WinFanfare", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
