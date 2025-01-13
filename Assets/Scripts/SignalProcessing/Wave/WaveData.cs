namespace Hex.SignalProcessing.Wave
{
    public class WaveData
    {
        public float[] SpectrumData { get; set; }
        public float Amplitude { get; set; }
        public float AgeInSeconds { get; set; }
        public float? RelativeAmplitude { get; set; }
    }
}
