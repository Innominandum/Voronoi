namespace Voronoi.Objects
{
    public interface RBNode
    {
        Point Site { get; set; }
        RBNode Previous { get; set; }
        RBNode Next { get; set; }
        RBNode Right { get; set; }
        RBNode Left { get; set; }
        RBNode Parent { get; set; }
        bool Red { get; set; }
    }
}
