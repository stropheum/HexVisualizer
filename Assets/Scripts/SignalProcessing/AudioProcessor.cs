using System;
using System.Collections;
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

        private AudioSource _audioSource;
        private float[] _spectrumData;
        private NativeArray<float> _samples;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private IEnumerator Start()
        {
            _audioSource.Play();
            yield return GetAudioData();
        }

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
                _samplesText.text = "SampleCount: " + sampleCount;
                float[] spectrum = new float[sampleCount];
                _audioSource.GetSpectrumData(spectrum, 0, _config.FftWindow);
                Draw.LineGeometry = LineGeometry.Volumetric3D;
                Draw.ThicknessSpace = ThicknessSpace.Meters;
                Draw.Thickness = 0.025f;
                Draw.Matrix = transform.localToWorldMatrix;

                for (int i = 1; i < spectrum.Length - 1; i++)
                {
                    Draw.Color = Color.green;
                    var start = new Vector3(Mathf.Log(i - 1), spectrum[i - 1] * _config.Amplitude, 0);
                    var end = new Vector3(Mathf.Log(i), spectrum[i] * _config.Amplitude, 0);
                    Draw.Line(drawOrigin + start, drawOrigin + end);
                }

                float logSampleCount = Mathf.Log(sampleCount-1);
                float lowPassX = drawOrigin.x + Mathf.Lerp(0f, logSampleCount, _config.LowPassFilter);
                float highPassX = drawOrigin.x + Mathf.Lerp(0f, logSampleCount, _config.HighPassFilter);
                Draw.Color = Color.magenta;
                Draw.Line(
                    new Vector3(lowPassX, drawOrigin.y, drawOrigin.z),
                    new Vector3(lowPassX, drawOrigin.y + 2f, drawOrigin.z));
                Draw.Color = Color.cyan;
                Draw.Line(
                    new Vector3(highPassX, drawOrigin.y, drawOrigin.z),
                    new Vector3(highPassX, drawOrigin.y + 2f, drawOrigin.z));
            }
        }
        
        private int CalculateSampleCount()
        {
            return (int)Mathf.Pow(2, _config.SampleCountPowerOf2);
        }

        private Vector3 CalculateDrawOrigin()
        {
            return transform.position + new Vector3(Mathf.Log(CalculateSampleCount()) / -2f, .5f, 0f);
        }

        private IEnumerator GetAudioData()
        {
            // Wait for sample data to be loaded
            while (_audioSource.clip.loadState != AudioDataLoadState.Loaded) yield return null;

            // Read all the samples from the clip and halve the gain
            int numSamples = _audioSource.clip.samples * _audioSource.clip.channels;
            _samples = new NativeArray<float>(numSamples, Allocator.Persistent);
            _audioSource.clip.GetData(_samples, 0);
            int frequency = _audioSource.clip.frequency;

            _spectrumData = new float[64];
            _audioSource.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris);
        }
    }
}