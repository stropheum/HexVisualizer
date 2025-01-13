using System.Collections.Generic;
using Shapes;
using UnityEngine;

namespace Hex.SignalProcessing.Wave
{
    [RequireComponent(typeof(AudioProcessor))]
    public class WavePropagator : ImmediateModeShapeDrawer
    {
        [SerializeField] private float _waveLifeSpanInSeconds = 15f;
        [SerializeField] private float _propagationSpeedMetersPerSecond = 1f;
        [SerializeField] private Gradient _amplitudeGradient = new();
        [SerializeField] [Range(1f, 20f)] private float _amplitudeMultiplier = 1f;
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
            for (int i = 0; i < _waveData.Count; i++)
            {
                _waveData[i].AgeInSeconds += Time.fixedDeltaTime;
            }

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
                    float percent = (wave.Amplitude - _activeMinimum) / range;
                    wave.RelativeAmplitude ??= percent;
                    Color color = _amplitudeGradient.Evaluate(wave.RelativeAmplitude.Value);
                    Draw.Torus(transform.position + new Vector3(0f, wave.RelativeAmplitude.Value * _amplitudeMultiplier, 0f), 
                        transform.up, 
                        wave.AgeInSeconds * _propagationSpeedMetersPerSecond, 
                        1f,
                        color);
                }
            }
        }

        private void AudioProcessorOnSpectrumDataEmitted(float[] spectrumData, float amplitude)
        {
            _waveData.Add(new WaveData
            {
                SpectrumData = spectrumData,
                Amplitude = amplitude,
                AgeInSeconds = 0f
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
    }
}
