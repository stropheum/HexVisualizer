using System;
using UnityEngine;

namespace Hex.SignalProcessing
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioProcessor : MonoBehaviour
    {
        [SerializeField] private FFTWindow _fftWindow;
        [SerializeField] [Range(6, 10)] private int _sampleCountPowerOf2;
        
        [field: SerializeField]
        [field: Range(0f, 1f)]
        public float LowPassFilter { get; private set; }

        [field: SerializeField]
        [field: Range(0f, 1f)]
        public float HighPassFilter { get; private set; } = 1f;

        public int SampleCount { get; private set; }
        public event Action<float[]> SpectrumDataEmitted;
        
        private float[] _spectrumData;

        private void Awake()
        {
            InitializeSpectrumData();
        }

        private void Update()
        {
            if (AudioSettings.speakerMode == AudioSpeakerMode.Stereo)
            {
                float[] left = new float[SampleCount];
                float[] right = new float[SampleCount];
                AudioListener.GetSpectrumData(left, 0, FFTWindow.BlackmanHarris);
                AudioListener.GetSpectrumData(right, 1, FFTWindow.BlackmanHarris);
                for (int i = 0; i < SampleCount; i++)
                {
                    _spectrumData[i] = (left[i] + right[i]) / 2.0f;                                                            
                }
            }
            else
            {
                AudioListener.GetSpectrumData(_spectrumData, 0 ,_fftWindow);
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