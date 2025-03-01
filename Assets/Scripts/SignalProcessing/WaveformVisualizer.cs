using Shapes;
using UnityEngine;

namespace Hex.SignalProcessing
{
    public class WaveformVisualizer : ImmediateModeShapeDrawer
    {
        [SerializeField] private AudioProcessor _audioProcessor;
        [SerializeField] private Vector3 _drawOriginOffset;
        [SerializeField] private float _baseLineThickness;
        [SerializeField] private float _amplitudeScale = 1f;
        private float[] _spectrumData;

        private void Awake()
        {
            ValidateInspectorBindings();
        }

        private void Start()
        {
            _spectrumData = new float[_audioProcessor.SampleCount];
            _audioProcessor.SpectrumDataEmitted += AudioProcessorOnSpectrumDataEmitted;
        }

        public override void DrawShapes(Camera cam)
        {
            DrawLogScaledVisualizer(cam);
        }
        
        private void ValidateInspectorBindings()
        {
            Debug.Assert(_audioProcessor != null, nameof(_audioProcessor) + " != null");
        }
        
        private void AudioProcessorOnSpectrumDataEmitted(float[] spectrumData)
        {
            _spectrumData = spectrumData;
        }
        
        private void DrawLogScaledVisualizer(Camera cam)
        {
            if (_spectrumData == null || _spectrumData.Length != _audioProcessor.SampleCount)
            {
                Debug.LogError("_spectrumData == null || _spectrumData.Length != _audioProcessor.SampleCount);");
                return;
            }
            
            using (Draw.Command(cam))
            {
                Vector3 drawOrigin = CalculateDrawOrigin();
                int sampleCount = _audioProcessor.SampleCount;
                
                Draw.LineGeometry = LineGeometry.Volumetric3D;
                Draw.ThicknessSpace = ThicknessSpace.Pixels;
                Draw.Matrix = transform.localToWorldMatrix;
                
                float logSampleCount = Mathf.Log10(sampleCount - 1);
                float lowPassX = Mathf.Lerp(0f, logSampleCount, _audioProcessor.LowPassFilter);
                float highPassX = Mathf.Lerp(0f, logSampleCount, _audioProcessor.HighPassFilter);

                // draw backing panel
                Draw.Color = new Color(0.25f, 0.25f, 0.25f);
                Draw.Rectangle(
                    pos: _drawOriginOffset, 
                    width: 3.5f, 
                    height: 2.5f, 
                    pivot: RectPivot.Center, 
                    cornerRadius: 0.125f);
                Draw.Color = new Color(0.71f, 0.14f, 0.75f);
                Draw.RectangleBorder(
                    pos: _drawOriginOffset, 
                    width: 3.5f, 
                    height: 2.5f, 
                    pivot: RectPivot.Center, 
                    thickness: 4.0f, 
                    cornerRadius: 0.125f);

                Draw.Thickness = _baseLineThickness;
                for (int i = 0; i < _spectrumData.Length; i++)
                {
                    float logX = Mathf.Log10(i);
                    float percent = 1.0f - logX / Mathf.Log10(_spectrumData.Length - 1);
                    Draw.Thickness = Mathf.Pow(2, (int)(8 * percent)) * _baseLineThickness;
                    Draw.Color = logX < lowPassX || logX > highPassX ? Color.gray : Color.green;
                    
                    float scaledAmplitude = _spectrumData[i] * _amplitudeScale;
                    var s = new Vector3(logX, -scaledAmplitude, 0f);
                    var size = new Vector2(Mathf.Log10(i + 1) - logX, scaledAmplitude * 2.0f);
                    Draw.Rectangle(drawOrigin + s, size, RectPivot.Corner);
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
        
        private Vector3 CalculateDrawOrigin()
        {
            return new Vector3(Mathf.Log10(_audioProcessor.SampleCount) / -2f, 0f, 0f) + _drawOriginOffset;
        }
        
    }
}