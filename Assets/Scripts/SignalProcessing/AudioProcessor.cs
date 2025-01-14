using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using TMPro;
using Unity.Collections;
using UnityEngine;

namespace Hex.SignalProcessing
{
    [Serializable]
    public struct ProcessorConfiguration
    {
        [SerializeField] public FFTWindow FftWindow;
        [SerializeField] [Range(6, 10)] public int SampleCountPowerOf2;
        [SerializeField] [Range(0f, 10f)] public float Amplitude;
        [SerializeField] [Range(0f, 1f)] public float LowPassFilter;
        [SerializeField] [Range(0f, 1f)] public float HighPassFilter;
    }

    [RequireComponent(typeof(AudioSource))]
    public class AudioProcessor : ImmediateModeShapeDrawer
    {
        [SerializeField] private TextMeshPro _samplesText;
        [SerializeField] private ProcessorConfiguration _config = new()
        {
            SampleCountPowerOf2 = 10,
            Amplitude = 1f,
            LowPassFilter = 0f,
            HighPassFilter = 1f
        };

        public event Action<float[], float> SpectrumDataEmitted;

        private AudioSource _audioSource;
        private NativeArray<float> _samples;
        private float[] _spectrumData;
        private float _minAmplitude;
        private float _maxAmplitude;
        private float _cachedAmplitude;
        private int _previousTimeSampleTick;
        private bool _initialized;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private IEnumerator Start()
        {
            _audioSource.Play();
            yield return GetAudioData();
        }

        private void FixedUpdate()
        {
            if (!_initialized) { return; }

            _audioSource.GetSpectrumData(_spectrumData, 0, _config.FftWindow);
            SpectrumDataEmitted?.Invoke(_spectrumData, _cachedAmplitude);
        }
        
        private void OnAudioFilterRead(float[] data, int channels)
        {
            _cachedAmplitude = ComputeAmplitude(data);
        }

        private float ComputeAmplitude(float[] data)
        {
            if (data.Length == 0) { return 0; }
            float sum = 0f;
            foreach (float sample in data)
            {
                sum += Mathf.Abs(sample);
            }
            return sum / data.Length;
        }

        // private void Update()
        // {
        //     if (!_initialized) { return; }
        //     int samplesToProcess = _audioSource.timeSamples - _previousTimeSampleTick;
        //     if (samplesToProcess == 0) { return; }
        //     int startIndex = _audioSource.timeSamples - samplesToProcess;
        //     float sum = 0f;
        //     for (int i = 0; i < samplesToProcess; i++)
        //     {
        //         int index = startIndex + i;
        //         sum += _samples[index];
        //     }
        //
        //     float amplitude = (sum / samplesToProcess);
        //     // float normalizedAmplitude = (amplitude - _minAmplitude) / (_maxAmplitude - _minAmplitude);
        //     _audioSource.GetSpectrumData(_spectrumData, 0, _config.FftWindow);
        //     // SpectrumDataEmitted?.Invoke(_spectrumData, normalizedAmplitude);
        //     SpectrumDataEmitted?.Invoke(_spectrumData, amplitude);
        //     _samplesText.text = "Raw: " + amplitude.ToString("0.0000") + "\nMin: " + _minAmplitude + "\nMax: " + _maxAmplitude + "\n Amplitude: " + amplitude.ToString("0.00");
        //     _previousTimeSampleTick = _audioSource.timeSamples;
        // }

        private void OnDestroy()
        {
            _samples.Dispose();
        }

        public override void DrawShapes(Camera cam)
        {
            using (Draw.Command(cam))
            {
                Vector3 drawOrigin = CalculateDrawOrigin();
                int sampleCount = CalculateSampleCount();
                float[] spectrum = new float[sampleCount];
                _audioSource.GetSpectrumData(spectrum, 0, _config.FftWindow);
                
                Draw.LineGeometry = LineGeometry.Volumetric3D;
                Draw.ThicknessSpace = ThicknessSpace.Meters;
                Draw.Thickness = 0.025f;
                Draw.Matrix = transform.localToWorldMatrix;

                for (int i = 1; i < spectrum.Length - 1; i++)
                {
                    Draw.Color = Color.green;
                    var start = new Vector3(Mathf.Log(i - 1), 0f, spectrum[i - 1] * _config.Amplitude);
                    var end = new Vector3(Mathf.Log(i), 0f, spectrum[i] * _config.Amplitude);
                    Draw.Line(drawOrigin + start, drawOrigin + end);
                }

                float logSampleCount = Mathf.Log(sampleCount-1);
                float lowPassX = drawOrigin.x + Mathf.Lerp(0f, logSampleCount, _config.LowPassFilter);
                float highPassX = drawOrigin.x + Mathf.Lerp(0f, logSampleCount, _config.HighPassFilter);
                Draw.Color = Color.magenta;
                Draw.Line(
                    new Vector3(lowPassX, drawOrigin.y, drawOrigin.z),
                    new Vector3(lowPassX, drawOrigin.y, drawOrigin.z + 2f));
                Draw.Color = Color.cyan;
                Draw.Line(
                    new Vector3(highPassX, drawOrigin.y, drawOrigin.z),
                    new Vector3(highPassX, drawOrigin.y, drawOrigin.z + 2f));
            }
        }
        
        private int CalculateSampleCount()
        {
            return (int)Mathf.Pow(2, _config.SampleCountPowerOf2);
        }

        private Vector3 CalculateDrawOrigin()
        {
            return transform.position + new Vector3(Mathf.Log(CalculateSampleCount()) / -2f, 125f, 0f);
        }

        private IEnumerator GetAudioData()
        {
            while (_audioSource.clip.loadState != AudioDataLoadState.Loaded) yield return null;

            int numSamples = _audioSource.clip.samples * _audioSource.clip.channels;
            _samples = new NativeArray<float>(numSamples, Allocator.Persistent);
            _audioSource.clip.GetData(_samples, 0);

            // Probably not necessary. Pre-warming _spectrumData with initial frame value
            _spectrumData = new float[64];
            _audioSource.GetSpectrumData(_spectrumData, 0, _config.FftWindow);
            _initialized = true;

            yield return null;
        }
        
        public static float[] SumArrays(List<float[]> arrays)
        {
            if (arrays == null || arrays.Count == 0)
                throw new ArgumentException("Array list cannot be null or empty.");

            int length = arrays[0].Length;

            // Ensure all arrays are of the same size
            if (!arrays.TrueForAll(a => a.Length == length))
                throw new ArgumentException("All arrays must have the same length.");

            float[] result = new float[length];

            foreach (float[] array in arrays)
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] += array[i];
                }
            }

            return result;
        }
    }
}