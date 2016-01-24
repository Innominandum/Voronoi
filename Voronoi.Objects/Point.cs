using System;
using System.Collections.Generic;

namespace Voronoi.Objects
{
    public class Point
    {
        #region Properties

        public int ID { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public double Noise { get; set; }
        
        public double Length
        {
            get
            {
                return Math.Sqrt(this.x * this.x + this.y * this.y);
            }
        }

        public Dictionary<Edge, Point> Neighbours = new Dictionary<Edge, Point>();

        #endregion

        /// <summary>
        /// This shows a representation of a particular point.
        /// </summary>
        public override string ToString()
        {
                return string.Format("[{0}x{1}x{2}]", this.x, this.y, this.z);
        }
    }
}
