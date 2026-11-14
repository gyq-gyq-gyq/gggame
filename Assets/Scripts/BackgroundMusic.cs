using UnityEngine;

[DisallowMultipleComponent]
public sealed class BackgroundMusic : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.22f;
    [SerializeField] private int bpm = 132;

    private AudioSource audioSource;
    private AudioClip musicClip;

    private void Awake()
    {
        EnsureAudioSource();
        musicClip = CreateMusicClip();
        audioSource.clip = musicClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
    }

    private void OnDestroy()
    {
        if (musicClip != null && Application.isPlaying)
        {
            Destroy(musicClip);
        }
    }

    private void EnsureAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private AudioClip CreateMusicClip()
    {
        const int sampleRate = 22050;
        const int totalBeats = 32;
        const float melodyNoteBeats = 0.5f;

        float beatLength = 60f / bpm;
        int sampleCount = Mathf.CeilToInt(totalBeats * beatLength * sampleRate);
        AudioClip clip = AudioClip.Create("LivelyBGM", sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];

        int[] melody = new int[]
        {
            60, 64, 67, 72, 71, 67, 64, 62,
            60, 64, 67, 72, 74, 72, 71, 67,
            69, 72, 76, 81, 79, 76, 72, 69,
            67, 71, 74, 79, 77, 74, 71, 67,
            65, 69, 72, 77, 76, 72, 69, 65,
            64, 67, 71, 76, 74, 71, 67, 64,
            62, 65, 69, 74, 72, 69, 65, 62,
            60, 64, 67, 72, 71, 67, 64, 60
        };

        int[] bass = new int[]
        {
            36, 33, 29, 31,
            36, 33, 29, 31,
            33, 29, 31, 36,
            33, 29, 31, 36
        };

        for (int i = 0; i < melody.Length; i++)
        {
            float startTime = i * melodyNoteBeats * beatLength;
            float duration = melodyNoteBeats * beatLength * 0.92f;
            AddTone(samples, sampleRate, MidiToFrequency(melody[i]), startTime, duration, 0.13f, true);
        }

        const float bassNoteBeats = 2f;
        for (int i = 0; i < bass.Length; i++)
        {
            float startTime = i * bassNoteBeats * beatLength;
            float duration = bassNoteBeats * beatLength * 0.9f;
            AddTone(samples, sampleRate, MidiToFrequency(bass[i]), startTime, duration, 0.10f, false);
        }

        for (int i = 0; i < 64; i++)
        {
            float startTime = i * melodyNoteBeats * beatLength;
            AddNoiseHat(samples, sampleRate, startTime, 0.035f, 0.012f);
        }

        Normalize(samples);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float MidiToFrequency(int midiNote)
    {
        return 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);
    }

    private static void AddTone(
        float[] samples,
        int sampleRate,
        float frequency,
        float startTime,
        float duration,
        float amplitude,
        bool squareWave)
    {
        int startSample = Mathf.RoundToInt(startTime * sampleRate);
        int endSample = Mathf.Min(samples.Length - 1, Mathf.RoundToInt((startTime + duration) * sampleRate));

        for (int i = startSample; i <= endSample; i++)
        {
            float t = i / (float)sampleRate;
            float localT = (i - startSample) / (float)(endSample - startSample + 1);
            float envelope = Mathf.Min(1f, Mathf.Min(localT * 24f, (1f - localT) * 8f));
            float phase = Mathf.Sin(Mathf.PI * 2f * frequency * t);
            float wave = squareWave ? Mathf.Sign(phase) * 0.5f : phase;
            samples[i] += wave * amplitude * Mathf.Max(0f, envelope);
        }
    }

    private static void AddNoiseHat(
        float[] samples,
        int sampleRate,
        float startTime,
        float duration,
        float amplitude)
    {
        int startSample = Mathf.RoundToInt(startTime * sampleRate);
        int endSample = Mathf.Min(samples.Length - 1, Mathf.RoundToInt((startTime + duration) * sampleRate));
        uint state = 1234567u;

        for (int i = startSample; i <= endSample; i++)
        {
            state = state * 1664525u + 1013904223u;
            float noise = ((state >> 8) & 0xffff) / 32767.5f - 1f;
            samples[i] += noise * amplitude;
        }
    }

    private static void Normalize(float[] samples)
    {
        float peak = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
        }

        if (peak <= 0.001f)
        {
            return;
        }

        float scale = 0.82f / peak;
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= scale;
        }
    }
}
