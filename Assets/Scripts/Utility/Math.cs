namespace Hex.Utility
{
    public static class Math
    {
        public static float Map(float value, float fromStart, float fromEnd, float toStart, float toEnd)
        {
            return toStart + ((toEnd - toStart) / (fromEnd - fromStart)) * (value - fromStart);
        }
    }
}
