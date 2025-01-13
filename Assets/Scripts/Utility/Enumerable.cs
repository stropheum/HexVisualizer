using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hex.Math
{
    public static class Enumerable
    {
        /// <summary>
        /// Calculates the root-mean-square of a value in a collection 
        /// </summary>
        /// <param name="values">The data collection</param>
        /// <param name="index">The index of the value being calculated</param>
        /// <returns>The root-mean-square value of the element at the provided index</returns>
        public static float RootMeanSquare(IEnumerable<float> values, int index)
        {
            float sum = 0f;
            float[] enumerable = values as float[] ?? values.ToArray();
            int count = enumerable.Length;
            for (int i = 0; i < count; i++)
            {
                int currentIndex = index + i;
                if (currentIndex < count)
                {
                    sum += enumerable[currentIndex] * enumerable[currentIndex]; // Square the sample
                }
            }

            return Mathf.Sqrt(sum / count); // RMS
        }
    }
}