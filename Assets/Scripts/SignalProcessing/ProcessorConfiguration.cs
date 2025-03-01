using System;
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
        [SerializeField] public float BaseLineThickness;
        [SerializeField] public Vector3 DrawOriginOffset;
    }
}