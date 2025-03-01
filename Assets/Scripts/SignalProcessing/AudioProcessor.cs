using System;
using System.Collections;
using System.Collections.Generic;
using Shapes;
using Unity.Collections;
using UnityEngine;

namespace Hex.SignalProcessing
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioProcessor : ImmediateModeShapeDrawer
    {
        [SerializeField] private ProcessorConfiguration _config = new()
        {
            SampleCountPowerOf2 = 10,
            Amplitude = 1f,
            LowPassFilter = 0f,
            HighPassFilter = 1f,
            BaseLineThickness = 1f,
            DrawOriginOffset = Vector3.zero
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
            foreach (float sample in data) { sum += Mathf.Abs(sample); }

            return sum / data.Length;
        }

        private void OnDestroy()
        {
            _samples.Dispose();
        }

        public override void DrawShapes(Camera cam)
        {
            DrawLogScaledVisualizer(cam);
        }

        private void DrawLogScaledVisualizer(Camera cam)
        {
            using (Draw.Command(cam))
            {
                Vector3 drawOrigin = CalculateDrawOrigin();
                int sampleCount = GetSpectrumSampleCount();
                float[] spectrum = new float[sampleCount];
                _audioSource.GetSpectrumData(spectrum, 0, _config.FftWindow);
                
                Draw.LineGeometry = LineGeometry.Volumetric3D;
                Draw.ThicknessSpace = ThicknessSpace.Pixels;
                Draw.Matrix = transform.localToWorldMatrix;
                
                float logSampleCount = Mathf.Log10(sampleCount - 1);
                float lowPassX = Mathf.Lerp(0f, logSampleCount, _config.LowPassFilter);
                float highPassX = Mathf.Lerp(0f, logSampleCount, _config.HighPassFilter);

                // draw backing panel
                Draw.Color = new Color(0.25f, 0.25f, 0.25f);
                Draw.Rectangle(
                    pos: transform.position + _config.DrawOriginOffset, 
                    width: 4f, 
                    height: 3f, 
                    pivot: RectPivot.Center, 
                    cornerRadius: 0.25f);
                Draw.Color = new Color(0.71f, 0.14f, 0.75f);
                Draw.RectangleBorder(
                    pos: transform.position + _config.DrawOriginOffset, 
                    width: 4f, 
                    height: 3f, 
                    pivot: RectPivot.Center, 
                    thickness: 16.0f, 
                    cornerRadius: 0.25f);

                Draw.Thickness = _config.BaseLineThickness;
                for (int i = 0; i < spectrum.Length; i++)
                {
                    float logX = Mathf.Log10(i);
                    float percent = 1.0f - logX / Mathf.Log10(spectrum.Length - 1);
                    // Draw.Thickness = Mathf.Pow(2, (int)(8 * percent)) * _config.BaseLineThickness;
                    Draw.Color = logX < lowPassX || logX > highPassX ? Color.gray : Color.green;
                    float amplitude = spectrum[i] * _config.Amplitude;
                    var start = new Vector3(logX, -amplitude, 0f);
                    var end = new Vector3(logX, amplitude, 0f);
                    
                    // Draw.Line(drawOrigin + start, drawOrigin + end);
                    var s = new Vector3(logX, -amplitude, 0f);
                    var size = new Vector2(Mathf.Log10(i + 1) - logX, amplitude * 2.0f);
                    Draw.Rectangle(drawOrigin + s, size, RectPivot.Corner);
                }
                
                Draw.Thickness = 4f;
                Draw.Color = Color.magenta;
                Draw.Line(
                    drawOrigin + new Vector3(lowPassX, -1.25f, 0f),
                    drawOrigin + new Vector3(lowPassX, 1.25f, 0f));
                Draw.Color = Color.cyan;
                Draw.Line(
                    drawOrigin + new Vector3(highPassX, -1.25f, 0f),
                    drawOrigin + new Vector3(highPassX, 1.25f, 0f));
            }
        }

        private int GetSpectrumSampleCount()
        {
            return (int)Mathf.Pow(2, _config.SampleCountPowerOf2);
        }

        private Vector3 CalculateDrawOrigin()
        {
            return transform.position + new Vector3(Mathf.Log10(GetSpectrumSampleCount()) / -2f, 0f, 0f) + _config.DrawOriginOffset;
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
            if (arrays == null || arrays.Count == 0) { throw new ArgumentException("Array list cannot be null or empty."); }

            int length = arrays[0].Length;

            // Ensure all arrays are of the same size
            if (!arrays.TrueForAll(a => a.Length == length)) { throw new ArgumentException("All arrays must have the same length."); }

            float[] result = new float[length];
            foreach (float[] array in arrays)
            {
                for (int i = 0; i < length; i++) result[i] += array[i];
            }

            return result;
        }
    }
}