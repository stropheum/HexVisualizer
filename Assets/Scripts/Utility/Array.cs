using System;
using System.Collections.Generic;

namespace Hex.Utility
{
    public static class Array
    {
        public static float[] SumArrays(List<float[]> arrays)
        {
            if (arrays == null || arrays.Count == 0) { throw new ArgumentException("Array list cannot be null or empty."); }

            int length = arrays[0].Length;
            if (!arrays.TrueForAll(a => a.Length == length)) { throw new ArgumentException("All arrays must have the same length."); }

            float[] result = new float[length];
            foreach (float[] array in arrays)
            {
                for (int i = 0; i < length; i++) result[i] += array[i];
            }

            return result;
        }
    }
}