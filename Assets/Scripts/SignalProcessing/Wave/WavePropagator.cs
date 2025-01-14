using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shapes;
using TMPro;
using UnityEngine;

namespace Hex.SignalProcessing.Wave
{
    [RequireComponent(typeof(AudioProcessor))]
    public class WavePropagator : ImmediateModeShapeDrawer
    {
        [SerializeField] private TextMeshProUGUI _debugText;
        [SerializeField] private float _waveLifeSpanInSeconds = 15f;
        [SerializeField] private float _propagationSpeedMetersPerSecond = 1f;
        [SerializeField] private Gradient _amplitudeGradient = new();
        [SerializeField] [Range(1f, 20f)] private float _amplitudeMultiplier = 1f;
        [SerializeField] private float _minAmplitude;
        [SerializeField] private int _lowPassFilter;
        [SerializeField] private int _highPassFilter;
        
        private AudioProcessor _audioProcessor;
        private readonly List<WaveData> _waveData = new();
        private float _activeMinimum = float.MaxValue;
        private float _activeMaximum = float.MinValue;

        private void Awake()
        {
            _audioProcessor = GetComponent<AudioProcessor>();
            _audioProcessor.SpectrumDataEmitted += AudioProcessorOnSpectrumDataEmitted;
        }

        private void FixedUpdate()
        {
            // TODO: doesn't seem to sync at the moment. possible gate to check if elapsed time exceeds sample rate
            foreach (WaveData wave in _waveData) { wave.AgeInSeconds += Time.fixedDeltaTime; }

            var waveDataCopy = new WaveData[_waveData.Count];
            _waveData.CopyTo(waveDataCopy);
            bool dirty = false;
            foreach (WaveData wave in waveDataCopy)
            {
                if (!(wave.AgeInSeconds >= _waveLifeSpanInSeconds)) { continue; }
                _waveData.Remove(wave);
                dirty = true;
            }
            
            if (dirty)
            {
                UpdateActiveMinMax();
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            _waveData.Clear();
        }
        
        public override void DrawShapes(Camera cam)
        {
            using (Draw.Command(cam))
            {
                Draw.LineGeometry = LineGeometry.Volumetric3D;
                Draw.ThicknessSpace = ThicknessSpace.Meters;
                Draw.Thickness = 0.001f;
                Draw.Matrix = transform.localToWorldMatrix;
                Draw.Opacity = 1f;
                
                float range = _activeMaximum - _activeMinimum;
                if (range == 0) { return; }
                
                foreach (WaveData wave in _waveData)
                {
                    if (wave.Amplitude < _minAmplitude) { continue; }
                    
                    float percent = (wave.Amplitude - _activeMinimum) / range;
                    wave.RelativeAmplitude ??= percent;
                    Draw.Torus(transform.position + new Vector3(0f, wave.RelativeAmplitude.Value * _amplitudeMultiplier, 0f), 
                        transform.up, 
                        wave.AgeInSeconds * _propagationSpeedMetersPerSecond, 
                        1f,
                        wave.Color);
                }
            }
        }

        private Color GenerateColorFromDominantFrequency(Gradient amplitudeGradient, float[] waveSpectrumData)
        {
            int maxIndex = -1;
            float maxValue = float.MinValue;
            int length = waveSpectrumData.Length;
            for (int i = 0; i < length; i++)
            {
                if (!(waveSpectrumData[i] > maxValue)) { continue; }
                maxValue = waveSpectrumData[i];
                maxIndex = i;
            }

            float percent = maxIndex / (float)length;
            _debugText.color = waveSpectrumData.Any(x => x < 0f) ? Color.red : Color.green;
            _debugText.text = "Max Index: " + maxIndex + "\nMaxValue: " + maxValue + "\nPercent: " + percent * 100f + "%" + "\nLength: " + length + "\n";
            return amplitudeGradient.Evaluate(percent);
        }

        private void AudioProcessorOnSpectrumDataEmitted(float[] spectrumData, float amplitude)
        {
            _waveData.Add(new WaveData
            {
                SpectrumData = spectrumData,
                Amplitude = amplitude,
                AgeInSeconds = 0f,
                Color = GenerateColorFromDominantFrequency(_amplitudeGradient, spectrumData)
            });
            if (amplitude > _activeMaximum) { _activeMaximum = amplitude; }
            if (amplitude < _activeMinimum) { _activeMinimum = amplitude; }
        }

        private void UpdateActiveMinMax()
        {
            _activeMinimum = float.MaxValue;
            _activeMaximum = float.MinValue;
            foreach (WaveData wave in _waveData)
            {
                if (wave.Amplitude > _activeMaximum) { _activeMaximum = wave.Amplitude; }
                if (wave.Amplitude < _activeMinimum) { _activeMinimum = wave.Amplitude; }
            }
        }

        private float SmoothAmplitude(float currentAmplitude, float lastAmplitude, float smoothingFactor)
        {
            return Mathf.Lerp(lastAmplitude, currentAmplitude, smoothingFactor * Time.deltaTime);
        }

        private float SmoothAmplitudeFixedDelta(float currentAmplitude, float lastAmplitude, float smoothingFactor)
        {
            return Mathf.Lerp(lastAmplitude, currentAmplitude, smoothingFactor * Time.fixedDeltaTime);
        }
    }
}
