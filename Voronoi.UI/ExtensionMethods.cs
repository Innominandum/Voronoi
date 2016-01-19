using System;

namespace Voronoi.UI
{
    public static class ExtensionMethods
    {
        public static double NextDoubleRange(this Random objRandom, double dblMin, double dblMax)
        {
            return objRandom.NextDouble() * (dblMax - dblMin) + dblMin;
        }
    }
}