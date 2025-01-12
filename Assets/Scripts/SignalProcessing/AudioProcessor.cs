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
        [SerializeField][Range(0f, 10f)] private float _amplitude = 1f;
        [SerializeField] private FFTWindow _fftWindow = FFTWindow.Rectangular;
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
            float currentAudioTime = _audioSource.time; // Time in seconds
            int currentAudioSample = _audioSource.timeSamples; // Sample index
            int sampleRate = AudioSettings.outputSampleRate;
            
            float rmsAmplitude = CalculateRootMeanSquare(currentAudioSample);
            // _samplesText.text = "Time: " + currentAudioTime + "\nCurrentSample: " + currentAudioSample + "\nSampleRate: " + sampleRate + "\nRMS: " + rmsAmplitude;
            // transform.position = new Vector3(transform.position.x, rmsAmplitude * _amplitude, transform.position.z);
            
            var spectrum = new float[256];
            _audioSource.GetSpectrumData(spectrum, 0, _fftWindow);
            float sum = 0f;
            for (int i = 0; i < spectrum.Length; i++)
            {
                // float value = spectrum[i] * _amplitude;
                // sum += value * value;
                sum += spectrum[i] * _amplitude;
            }
            float avg = sum / spectrum.Length;
            _samplesText.text = "AVG of spectrum frame: " + avg + "\nAmplitude: " + _amplitude;
            transform.position = new Vector3(transform.position.x, avg, transform.position.z);
            
            // float rms = Mathf.Sqrt(sum / spectrum.Length);
            // _samplesText.text = "RMS of spectrum frame: " + rms + "\nAmplitude: " + _amplitude;
            // transform.position = new Vector3(transform.position.x, rms, transform.position.z);

            // DebugDrawSpectrumData();
            // float elapsedTime = (float)_audioSource.timeSamples / sampleRate;
            //
            // float fps = 1f / Time.deltaTime;
            // _averageFps = (_averageFps + fps) * .5f;
            // Debug.Log("framerate: " + fps);
            // int currentFrame = Mathf.FloorToInt(elapsedTime * fps);
            // transform.position = new Vector3(transform.position.x, _samples[currentFrame], transform.position.z);
            // _samplesText.text = _samples[currentFrame].ToString(CultureInfo.InvariantCulture);
        }

        // private void DebugDrawSpectrumData()
        // {
        //     var spectrum = new float[256];
        //     _audioSource.GetSpectrumData(spectrum, 0, _fftWindow);
        //
        //     for (int i = 1; i < spectrum.Length - 1; i++)
        //     {
        //         Debug.DrawLine(new Vector3(i - 1, spectrum[i] + 10, 0), new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
        //         Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
        //         Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
        //         Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
        //     }
        // }

        public override void DrawShapes(Camera cam)
        {
            using (Draw.Command(cam))
            {
                var spectrum = new float[256];
                _audioSource.GetSpectrumData(spectrum, 0, _fftWindow);
                Draw.LineGeometry = LineGeometry.Volumetric3D;
                Draw.ThicknessSpace = ThicknessSpace.Meters;
                Draw.Thickness = 0.05f;

                Draw.Matrix = transform.localToWorldMatrix;
                
                for (int i = 1; i < spectrum.Length - 1; i++)
                {
                    Draw.Color = Color.red;
                    Draw.Line(new Vector3(i - 1, spectrum[i] * _amplitude + 10, 0), new Vector3(i, spectrum[i + 1] * _amplitude + 10, 0));
                    Draw.Color = Color.cyan;
                    Draw.Line(new Vector3(i - 1, Mathf.Log(spectrum[i - 1] * _amplitude) + 10, 2), new Vector3(i, Mathf.Log(spectrum[i] * _amplitude) + 10, 2));
                    Draw.Color = Color.green;
                    Draw.Line(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] * _amplitude - 10, 1), new Vector3(Mathf.Log(i), spectrum[i] * _amplitude - 10, 1));
                    Draw.Color = Color.blue;
                    Draw.Line(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1] * _amplitude), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i] * _amplitude), 3));
                }                
            }
        }

        private float CalculateRootMeanSquare(int sampleIndex, int sampleWindow = 1024)
        {
            float sum = 0f;
            for (int i = 0; i < sampleWindow; i++)
            {
                int index = sampleIndex + i;
                if (index < _samples.Length)
                {
                    sum += _samples[index] * _samples[index]; // Square the sample
                }
            }
            return Mathf.Sqrt(sum / sampleWindow); // RMS
        }
        
        // private void FixedUpdate()
        // {
        //     // // UpdateSpectrumDataText();
        //     //
        //     // float currentAudioTime = _audioSource.time; // Time in seconds
        //     // int currentAudioSample = _audioSource.timeSamples; // Sample index
        //     // int sampleRate = AudioSettings.outputSampleRate;
        //     //
        //     // float elapsedTime = (float)_audioSource.timeSamples / sampleRate;
        //     //
        //     // float fps = Time.fixedDeltaTime;
        //     // Debug.Log("framerate: " + fps);
        //     // int currentFrame = Mathf.FloorToInt(elapsedTime * fps);
        //     transform.position = new Vector3(transform.position.x, _samples[currentFrame], transform.position.z);
        //     _samplesText.text = _samples[currentFrame].ToString(CultureInfo.InvariantCulture);
        // }

        private void OnDestroy()
        {
            _samples.Dispose();
        }

        // private void UpdateSpectrumDataText()
        // {
        //     // _samplesText.text = _spectrumData.ToString();
        //     _samplesText.text = string.Empty;
        //     var text = string.Empty;
        //     foreach (float data in _spectrumData)
        //     {
        //         text += data.ToString("0.00") + " ";
        //     }
        //
        //     _samplesText.text = text;
        // }

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
    
            // for (int i = 0; i < samples.Length; ++i)
            // {
            //     samples[i] *= 0.5f;
            // }
            //
            // _audioSource.clip.SetData(samples, 0);
        }
    }
}
