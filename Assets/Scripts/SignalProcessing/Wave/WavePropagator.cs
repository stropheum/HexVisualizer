using System;
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
        [SerializeField] [Range(0f, 1f)] private float _lowPassFilter;
        [SerializeField] [Range(0f, 1f)] private float _highPassFilter;
        
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
        // Prepare drawing settings
        Draw.LineGeometry = LineGeometry.Volumetric3D;
        Draw.ThicknessSpace = ThicknessSpace.Meters;
        Draw.Thickness = 0.001f;
        Draw.Matrix = transform.localToWorldMatrix;
        Draw.Opacity = 1f;

        // Calculate the active range for amplitude normalization
        float range = _activeMaximum - _activeMinimum;
        if (range == 0) { return; }

        // Draw the torus waves
        foreach (WaveData wave in _waveData)
        {
            if (wave.Amplitude < _minAmplitude) { continue; }
            float percent = (wave.Amplitude - _activeMinimum) / range;
            wave.RelativeAmplitude ??= percent;
            Draw.Torus(transform.position + new Vector3(0f, wave.RelativeAmplitude.Value * _amplitudeMultiplier, 0f),
                transform.up, wave.AgeInSeconds * _propagationSpeedMetersPerSecond, 1f, wave.Color);
        }
        
        Vector3 drawOrigin = _audioProcessor.CalculateDrawOrigin();
        int sampleCount = _audioProcessor.GetSpectrumSampleCount();
                
        Draw.Thickness = 0.025f;
        
        float logSampleCount = Mathf.Log(sampleCount-1);
        float lowPassX = drawOrigin.x + Mathf.Lerp(0f, logSampleCount, _lowPassFilter);
        float highPassX = drawOrigin.x + Mathf.Lerp(0f, logSampleCount, _highPassFilter);
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

        
        /// <summary>
        /// Maps the slider percent value to an appropriate frequency range accounting for logarithmic drop-off
        /// </summary>
        /// <param name="sliderValue">The value of the slider</param>
        /// <param name="numChannels">The number of frequency channels in the spectrum data</param>
        /// <returns></returns>
        private static int MapSliderToIndex(float sliderValue, int numChannels)
        {
            float logMin = Mathf.Log(1);           // log(1) = 0 (safe min index)
            float logMax = Mathf.Log(numChannels); // log of max index

            // Map slider value (0-1) to a log-scale index
            float logIndex = Mathf.Lerp(logMin, logMax, sliderValue);

            // Convert back from log-space to linear index
            int index = Mathf.RoundToInt(Mathf.Exp(logIndex));

            return Mathf.Clamp(index, 0, numChannels - 1);
        }


        private Color GenerateColorFromChannel(Gradient amplitudeGradient, float[] waveSpectrumData, int channel)
        {
            int length = waveSpectrumData.Length;
            float maxValue = waveSpectrumData[channel];
            float percent = channel / (float)length;
            _debugText.color = waveSpectrumData.Any(x => x < 0f) ? Color.red : Color.green;
            _debugText.text = "Max Index: " + channel + "\nMaxValue: " + maxValue + "\nPercent: " + percent * 100f + "%" + "\nLength: " + length + "\n";
            return amplitudeGradient.Evaluate(percent);
        }

        private static int CalculateDominantFrequencyChannel(float[] waveSpectrumData)
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

            return maxIndex;
        }

        private void AudioProcessorOnSpectrumDataEmitted(float[] spectrumData, float amplitude)
        {
            float logNumChannels = Mathf.Log(spectrumData.Length - 1);
            int dominantFrequencyChannel = CalculateDominantFrequencyChannel(spectrumData);
            float lpThreshold = _lowPassFilter * logNumChannels;
            float hpThreshold = _highPassFilter * logNumChannels;
            
            if (dominantFrequencyChannel < lpThreshold || dominantFrequencyChannel >= hpThreshold) { return; }
            _waveData.Add(new WaveData
            {
                SpectrumData = spectrumData,
                Amplitude = amplitude,
                AgeInSeconds = 0f,
                Color = GenerateColorFromChannel(_amplitudeGradient, spectrumData, dominantFrequencyChannel)
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
