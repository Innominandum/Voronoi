using System;

namespace Voronoi
{
    namespace Enumerations
    {
        public enum Biomes
        {
            Water = 1,
            Land = 2,
            Ocean = 3
        }

        public enum ElevationZones
        {
            High,
            UpperMiddle,
            LowerMiddle,
            Low,
            Shallow,
            Deep,
            Trench
        }

        public enum EdgeType
        {
            None,
            Outer,
            Coast,
            River,
            Land,
            Water
        }
    }
}
