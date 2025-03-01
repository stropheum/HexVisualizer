using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Hex.SignalProcessing
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(AudioLowPassFilter))]
    [RequireComponent(typeof(AudioHighPassFilter))]
    public class AudioProcessor : MonoBehaviour
    {
        private const float MinFrequency = 10.0f;
        private const float MaxFrequency = 22000.0f;
        
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private FFTWindow _fftWindow;
        [SerializeField] [Range(6, 10)] private int _sampleCountPowerOf2;

        [field: SerializeField]
        [field: Range(0f, 1f)]
        public float LowPassFilter { get; private set; } = 1;

        [field: SerializeField]
        [field: Range(0f, 1f)]
        public float HighPassFilter { get; private set; }

        public int SampleCount { get; private set; }
        public event Action<float[]> SpectrumDataEmitted;
        
        private float[] _spectrumData;
        private AudioSource _audioSource;
        private AudioLowPassFilter _audioLowPassFilter;
        private AudioHighPassFilter _audioHighPassFilter;

        private void Awake()
        {
            Debug.Assert(_audioMixer != null, nameof(_audioMixer) + " != null");
            _audioSource = GetComponent<AudioSource>();
            _audioLowPassFilter = GetComponent<AudioLowPassFilter>();
            _audioHighPassFilter = GetComponent<AudioHighPassFilter>();
            InitializeSpectrumData();
        }

        private void Update()
        {
            float lowPassCutoff = Utility.Math.Map(LowPassFilter, 0f, 1f, MinFrequency, MaxFrequency);
            _audioLowPassFilter.cutoffFrequency = lowPassCutoff;
            float highPassCutoff = Utility.Math.Map(HighPassFilter, 0f, 1f, MinFrequency, MaxFrequency);
            _audioHighPassFilter.cutoffFrequency = highPassCutoff;
            
            if (AudioSettings.speakerMode == AudioSpeakerMode.Stereo)
            {
                float[] left = new float[SampleCount];
                float[] right = new float[SampleCount];
                _audioSource.GetSpectrumData(left, 0, _fftWinodw);
                _audioSource.GetSpectrumData(right, 1, _fftWindow);
                for (int i = 0; i < SampleCount; i++)
                {
                    _spectrumData[i] = (left[i] + right[i]) / 2.0f;                                                            
                }
            }
            else
            {
                _audioSource.GetSpectrumData(_spectrumData, 0, _fftWindow);
            }
            
            SpectrumDataEmitted?.Invoke(_spectrumData);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                InitializeSpectrumData();
            }
        }

        private void InitializeSpectrumData()
        {
            SampleCount = (int)Mathf.Pow(2, _sampleCountPowerOf2);
            _spectrumData = new float[SampleCount];
        }

    }
}