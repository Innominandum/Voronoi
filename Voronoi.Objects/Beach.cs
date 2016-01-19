namespace Voronoi.Objects
{
    public class Beach : RBNode
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

        public Circle Circle;
        public Edge Edge;

        #endregion
    }
}
