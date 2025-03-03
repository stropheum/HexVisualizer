using System;
using Shapes;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Hex.SignalProcessing
{
    public enum VisualizerMode
    {
        LogScaled,
        Linearized
    }

    public class WaveformVisualizer : ImmediateModeShapeDrawer
    {
        [SerializeField] private VisualizerMode _visualizerMode = VisualizerMode.Linearized;
        [SerializeField] private AudioProcessor _audioProcessor;
        [SerializeField] private Vector3 _drawOriginOffset;
        [SerializeField] private float _baseLineThickness;
        [SerializeField] private float _amplitudeScale = 1f;
        [SerializeField] [Range(1, 256)] private int _frequencyBandCount;
        [SerializeField] private Vector2 _windowSize = new(3.5f, 2.5f);
        [SerializeField] private Vector2 _windowPadding = new(0.5f, 0.25f);

        private Tuple<int, int>[] _frequencyBandRanges;
        private float[] _frequencyBands;
        private float[] _spectrumData;

        private void Awake()
        {
            ValidateInspectorBindings();
        }

        private void Start()
        {
            _spectrumData = new float[_audioProcessor.SampleCount];
            TryUpdateLinearizedModel();
            _audioProcessor.SpectrumDataEmitted += AudioProcessorOnSpectrumDataEmitted;
        }

        private void OnValidate()
        {
            TryUpdateLinearizedModel();
        }

        public override void DrawShapes(Camera cam)
        {
            switch (_visualizerMode)
            {
                case VisualizerMode.LogScaled:
                    DrawLogScaledVisualizer(cam);
                    break;
                case VisualizerMode.Linearized:
                    DrawLinearizedVisualizer(cam);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ValidateInspectorBindings()
        {
            Debug.Assert(_audioProcessor != null, nameof(_audioProcessor) + " != null");
        }

        private void AudioProcessorOnSpectrumDataEmitted(float[] spectrumData)
        {
            _spectrumData = spectrumData;
        }

        private void DrawLinearizedVisualizer(Camera cam)
        {
            if (_spectrumData == null || _spectrumData.Length != _audioProcessor.SampleCount)
            {
                Debug.LogError("_spectrumData == null || _spectrumData.Length != _audioProcessor.SampleCount);");
                return;
            }

            using (Draw.Command(cam))
            {
                Vector3 drawOrigin = new Vector3(_windowSize.x * -0.5f, 0f, 0f) + _drawOriginOffset;

                Draw.LineGeometry = LineGeometry.Billboard;
                Draw.ThicknessSpace = ThicknessSpace.Pixels;
                Draw.Matrix = transform.localToWorldMatrix;
                
                float lowPassX = Mathf.Lerp(0f, _windowSize.x, _audioProcessor.LowPassFilter);
                float highPassX = Mathf.Lerp(0f, _windowSize.x, _audioProcessor.HighPassFilter);

                // draw backing panel
                Draw.Color = new Color(0.2f, 0.2f, 0.2f);
                Draw.Rectangle(
                    _drawOriginOffset,
                    _windowSize.x + _windowPadding.x,
                    _windowSize.y + _windowPadding.y,
                    RectPivot.Center,
                    0.125f);
                Draw.Color = Color.black;
                Draw.RectangleBorder(
                    _drawOriginOffset,
                    _windowSize.x + _windowPadding.x,
                    _windowSize.y + _windowPadding.y,
                    RectPivot.Center,
                    4.0f,
                    0.125f);

                const string labelText = "Visualizer Mode: Linearized";
                Vector3 textPosition = new Vector3(_windowSize.x, _windowSize.y, 0f) * -0.5f + _drawOriginOffset + new Vector3(0.125f, 0.05f, 0f);
                var textColor = new Color(1, 1, 1, 0.75f);
                Draw.Text(content: labelText, align: TextAlign.BottomLeft, color: textColor, fontSize: 2f, pos: textPosition);

                for (int i = 0; i < _frequencyBandRanges.Length; i++)
                {
                    float sum = 0f;
                    int startIndex = _frequencyBandRanges[i].Item1;
                    int endIndex = _frequencyBandRanges[i].Item2;
                    for (int j = startIndex; j < endIndex; j++) sum += _spectrumData[j];
                    _frequencyBands[i] = sum;
                }

                Draw.Thickness = _baseLineThickness;
                float barThickness = _windowSize.x / _frequencyBandCount;
                for (int i = 0; i < _frequencyBandCount; i++)
                {
                    float percent = (float)i / _frequencyBandCount;
                    Draw.Color = percent > _audioProcessor.LowPassFilter || percent < _audioProcessor.HighPassFilter
                        ? Color.gray
                        : Color.green;

                    float scaledAmplitude = _frequencyBands[i] * Mathf.Log10(i + 2) * _amplitudeScale;
                    var size = new Vector2(barThickness, scaledAmplitude * 2.0f);
                    var barOffset = new Vector3(i * barThickness, -scaledAmplitude, 0f);
                    Draw.Rectangle(drawOrigin + barOffset, size, RectPivot.Corner);
                }
                
                Draw.Thickness = _baseLineThickness;
                Draw.Color = Color.magenta;
                Draw.Line(
                    drawOrigin + new Vector3(lowPassX, -1f, 0f),
                    drawOrigin + new Vector3(lowPassX, 1f, 0f));
                Draw.Color = Color.cyan;
                Draw.Line(
                    drawOrigin + new Vector3(highPassX, -1f, 0f),
                    drawOrigin + new Vector3(highPassX, 1f, 0f));
            }
        }

        private void DrawLogScaledVisualizer(Camera cam)
        {
            if (_spectrumData.Length != _audioProcessor.SampleCount)
            {
                Debug.LogError("_spectrumData == null || _spectrumData.Length != _audioProcessor.SampleCount);");
                return;
            }
            
            using DrawCommand dc = Draw.Command(cam);
            Vector3 drawOrigin = new Vector3(Mathf.Log10(_audioProcessor.SampleCount) / -2f, 0f, 0f) + _drawOriginOffset;
            int sampleCount = _audioProcessor.SampleCount;

            Draw.LineGeometry = LineGeometry.Billboard;
            Draw.ThicknessSpace = ThicknessSpace.Pixels;
            Draw.Matrix = transform.localToWorldMatrix;

            float logSampleCount = Mathf.Log10(sampleCount - 1);
            float lowPassX = Mathf.Lerp(0f, logSampleCount, _audioProcessor.LowPassFilter);
            float highPassX = Mathf.Lerp(0f, logSampleCount, _audioProcessor.HighPassFilter);
            
            Draw.Color = new Color(0.25f, 0.25f, 0.25f);
            Draw.Rectangle(
                _drawOriginOffset,
                3.5f,
                2.5f,
                RectPivot.Center,
                0.125f);
            Draw.Color = new Color(0.71f, 0.14f, 0.75f);
            Draw.RectangleBorder(
                _drawOriginOffset,
                3.5f,
                2.5f,
                RectPivot.Center,
                4.0f,
                0.125f);

            Draw.Thickness = _baseLineThickness;
            for (int i = 0; i < _spectrumData.Length; i++)
            {
                float logX = Mathf.Log10(i);
                float percent = 1.0f - logX / Mathf.Log10(_spectrumData.Length - 1);
                Draw.Thickness = Mathf.Pow(2, (int)(8 * percent)) * _baseLineThickness;
                Draw.Color = logX > lowPassX || logX < highPassX ? Color.gray : Color.green;

                float scaledAmplitude = _spectrumData[i] * _amplitudeScale;
                var barOffset = new Vector3(logX, -scaledAmplitude, 0f);
                var size = new Vector2(Mathf.Log10(i + 1) - logX, scaledAmplitude * 2.0f);
                Draw.Rectangle(drawOrigin + barOffset, size, RectPivot.Corner);
            }

            Draw.Thickness = _baseLineThickness;
            Draw.Color = Color.magenta;
            Draw.Line(
                drawOrigin + new Vector3(lowPassX, -1f, 0f),
                drawOrigin + new Vector3(lowPassX, 1f, 0f));
            Draw.Color = Color.cyan;
            Draw.Line(
                drawOrigin + new Vector3(highPassX, -1f, 0f),
                drawOrigin + new Vector3(highPassX, 1f, 0f));
        }

        private void TryUpdateLinearizedModel()
        {
            if (_visualizerMode != VisualizerMode.Linearized || _spectrumData == null) { return; }

            int spectrumLength = _spectrumData.Length;
            float nyquistFreq = AudioSettings.outputSampleRate * 0.5f;
            float binWidth = nyquistFreq / spectrumLength;

            float logBase = Mathf.Pow(AudioProcessor.MaxFrequency / AudioProcessor.MinFrequency, 1f / _frequencyBandCount);

            _frequencyBandRanges = new Tuple<int, int>[_frequencyBandCount];
            _frequencyBands = new float[_frequencyBandCount];

            int lastEndIndex = 0;

            for (int i = 0; i < _frequencyBandCount; i++)
            {
                float bandStart = AudioProcessor.MinFrequency * Mathf.Pow(logBase, i);
                float bandEnd = AudioProcessor.MinFrequency * Mathf.Pow(logBase, i + 1);

                int startIndex = Mathf.RoundToInt(bandStart / binWidth);
                int endIndex = Mathf.RoundToInt(bandEnd / binWidth);

                startIndex = Mathf.Max(lastEndIndex, startIndex);
                endIndex = Mathf.Max(startIndex + 2, endIndex);

                if (endIndex >= spectrumLength)
                {
                    endIndex = spectrumLength - 1;
                    if (endIndex - startIndex < 2)
                    {
                        startIndex = Mathf.Max(0, endIndex - 2);
                    }
                }

                _frequencyBandRanges[i] = Tuple.Create(startIndex, endIndex);
                lastEndIndex = endIndex;
            }
        }
    }
}