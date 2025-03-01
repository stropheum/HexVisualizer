namespace Hex.Utility
{
    public static class Math
    {
        public static float Map(float value, float from1, float from2, float to1, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}
