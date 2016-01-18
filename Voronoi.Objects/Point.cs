using System;

namespace Voronoi
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

        public System.Collections.Generic.Dictionary<Edge, Point> Neighbours = new System.Collections.Generic.Dictionary<Edge, Point>();

        #endregion

        /// <summary>
        /// This shows a representation of a particular point.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-08-10</date>
        public override string ToString()
        {
            try
            {
                return string.Format("[{0}x{1}x{2}]", this.x, this.y, this.z);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in ToString", ex);
            }
        }
    }
}
