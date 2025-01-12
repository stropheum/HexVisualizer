using System;
using System.Collections;
using System.Globalization;
using Shapes;
using TMPro;
using Unity.Collections;
using UnityEngine;

namespace Hex.SignalProcessing
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioProcessor : ImmediateModeShapeDrawer
    {
        [SerializeField] private TextMeshPro _samplesText;
        [SerializeField] [Range(0f, 10f)] private float _amplitude = 1f;
        [SerializeField] private FFTWindow _fftWindow = FFTWindow.Rectangular;
        [SerializeField] private int _sampleCount = 1024;
        [SerializeField] [Range(0f, 1f)] private float _lowPassFilter = 0.0f;
        [SerializeField] [Range(0f, 1f)] private float _highPassFilter = 1.0f;
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

        private void Update()
        {
            // TODO: Consider adding in low-pass and high-pass filters to reduce noise. Add parameterized spectrum filters to isolate frequency groups 
            // float currentAudioTime = _audioSource.time; // Time in seconds
            // int currentAudioSample = _audioSource.timeSamples; // Sample index
            // int sampleRate = AudioSettings.outputSampleRate;
            //
            // var spectrum = new float[_sampleCount];
            // _audioSource.GetSpectrumData(spectrum, 0, _fftWindow);
            // float sum = 0f;
            // for (int i = 0; i < spectrum.Length; i++)
            // {
            //     sum += spectrum[i] * _amplitude;
            // }
            // transform.position = new Vector3(transform.position.x, sum, transform.position.z);
        }
        
        private void OnDestroy()
        {
            _samples.Dispose();
        }

        public override void DrawShapes(Camera cam)
        {
            using (Draw.Command(cam))
            {
                var spectrum = new float[_sampleCount];
                _audioSource.GetSpectrumData(spectrum, 0, _fftWindow);
                Draw.LineGeometry = LineGeometry.Volumetric3D;
                Draw.ThicknessSpace = ThicknessSpace.Meters;
                Draw.Thickness = 0.025f;

                Draw.Matrix = transform.localToWorldMatrix;
                
                for (int i = 1; i < spectrum.Length - 1; i++)
                {
                    Draw.Color = Color.green;
                    Draw.Line(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] * _amplitude, 1), new Vector3(Mathf.Log(i), spectrum[i] * _amplitude, 1));
                }                
            }
        }

        private IEnumerator GetAudioData()
        {
            // Wait for sample data to be loaded
            while (_audioSource.clip.loadState != AudioDataLoadState.Loaded) { yield return null; }
    
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
