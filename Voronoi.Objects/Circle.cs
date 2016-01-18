using System;

namespace Voronoi
{
    public class Circle : RBNode
    {
        #region Interface

        public Point Site { get; set; }
        public RBNode Previous { get; set; }
        public RBNode Next { get; set; }
        public RBNode Right { get; set; }
        public RBNode Left { get; set; }
        public RBNode Parent { get; set; }
        public bool Red { get; set; }

        #endregion

        #region Properties

        public double x;
        public double y;
        public double ycenter;
        public Beach Arc;

        #endregion
    }
}
