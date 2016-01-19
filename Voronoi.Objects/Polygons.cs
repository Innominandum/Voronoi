using System;
using System.Collections.Generic;

namespace Voronoi.Objects
{
    public class Polygons
    {
        #region Properties

        public int Height { get; set; }
        public int Width { get; set; }
        public int Seed { get; set; }

        public List<Point> SiteList = new List<Point>();
        public Stack<Point> Sites = new Stack<Point>();
        public Dictionary<int, Cell> Cells = new Dictionary<int, Cell>();
        public List<Edge> Edges = new List<Edge>();

        private Box Box;
        private Circle FirstCircle = null;
        private RBTree BeachLines = new RBTree();
        private RBTree Circles = new RBTree();
        private Stack<Beach> BeachJunkyard = new Stack<Beach>();
        private Stack<Circle> CircleJunkyard = new Stack<Circle>();

        private const double Epsilon = 1e-9;
        private const double Infinity = 1e30;

        private Dictionary<string, Point> EdgeVertices = new Dictionary<string, Point>();

        #endregion

        #region Support

        private Boolean GreaterThanWithEpsilon(double a, double b) { return a - b > Polygons.Epsilon; }
        private Boolean LessThanWithEpsilon(double a, double b) { return b - a > Polygons.Epsilon; }
        private Boolean EqualWithEpsilon(double a, double b) { return Math.Abs(a - b) < Polygons.Epsilon; }
        private Boolean LessThanEpsilonAbs(double a, double b) { return this.EqualWithEpsilon(a, b); }

        #endregion

        /// <summary>
        /// This method can also be called to clear the object of any of its data from a previous run. You do this when you 
        /// want to create some blue noise. For instance with the Lloyd Relaxation iterations.
        /// </summary>
        /// <created>Dennis Steinmeijer</created>
        /// <date>2013-07-20</date>
        public void Reset()
        {
            this.SiteList.Clear();
            this.Sites.Clear();
            this.Cells.Clear();
            this.FirstCircle = null;
            this.Edges.Clear();
            this.BeachLines = new RBTree();
            this.Circles = new RBTree();
            this.BeachJunkyard.Clear();
            this.CircleJunkyard.Clear();
            this.EdgeVertices.Clear();
        }

        /// <summary>
        /// Compute the voronoi polygons.
        /// </summary>
        /// <created>Dennis Steinmeijer</created>
        /// <date>2013-07-20</date>
        public void Compute()
        {
            // Set the viewbox.
            this.Box = new Box()
            {
                xl = 0,
                xr = this.Width,
                yt = 0,
                yb = this.Height
            };

            // Check to see whether we have random sites.
            if (this.SiteList.Count < 1)
            {
                throw new Exception("No sites supplied.");
            }

            // Initialise some variables.
            int intSite = 0;
            double intSiteX = -Polygons.Infinity;
            double intSiteY = -Polygons.Infinity;

            // First we'll need to sort the list of sites.
            this.SiteList.Sort(SortSites);

            // Then we need to transfer the sites to the stack.
            foreach (Point objRandomSite in this.SiteList)
            {
                this.Sites.Push(objRandomSite);
            }

            // Grab the first site.
            Point objSite = this.Sites.Pop();

            // Keep going until we run out of sites or beaches.
            while (true)
            {
                // Grab the first circle event.
                Circle objCircle = this.FirstCircle;

                if (objSite != null && (objCircle == null || objSite.y < objCircle.y || (objSite.y == objCircle.y && objSite.x < objCircle.x)))
                {
                    // Let's make sure we don't happen to have two the exact same points. 
                    // If so, just ignore and continue with the next one.
                    if (objSite.x != intSiteX || objSite.y != intSiteY)
                    {
                        // Raise the site counter.
                        intSite++;

                        // Set the site counter as the ID of the site.
                        objSite.ID = intSite;

                        // Create a new cell.
                        Cell objCell = new Cell()
                        {
                            Site = objSite
                        };

                        // Add the cell to the collection.
                        this.Cells.Add(objSite.ID, objCell);

                        // Add the cell as a beach section.
                        this.AddBeach(objSite);

                        // Let's remember the coordinates of the last handled point.
                        intSiteX = objSite.x;
                        intSiteY = objSite.y;
                    }

                    if (this.Sites.Count < 1)
                    {
                        objSite = null;
                    }
                    else
                    {
                        objSite = this.Sites.Pop();
                    }
                }
                else if (objCircle != null)
                {
                    // Remove the beach section.
                    this.RemoveBeach(objCircle.Arc);
                }
                else
                {
                    break;
                }
            }

            // wrapping-up:
            //   connect dangling edges to bounding box
            //   cut edges as per bounding box
            //   discard edges completely outside bounding box
            //   discard edges which are point-like
            this.ClipEdges();

            //   add missing edges in order to close opened cells
            this.CloseCells();
        }

        /// <summary>
        /// A comparer to sort the site list.
        /// </summary>
        /// <created>Dennis Steinmeijer</created>
        /// <date>2013-07-20</date>
        private int SortSites(Point a, Point b)
        {
            double dblReturn = b.y - a.y;

            if (dblReturn == 0)
            {
                dblReturn = b.x - a.x;
            }

            if (dblReturn > 0)
            {
                return 1;
            }
            else if (dblReturn < 0)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// AddBeachSection
        /// </summary>
        /// <created>Dennis Steinmeijer</created>
        /// <date>2013-07-21</date>
        private void AddBeach(Point objSite)
        {
            // Initialise variables.
            Beach objNode = this.BeachLines.Root as Beach;
            Beach objLeftArc = null;
            Beach objRightArc = null;

            while (objNode != null)
            {
                double dblDXL = this.LeftBreakPoint(objNode, objSite.y) - objSite.x;

                if (dblDXL > Polygons.Epsilon)
                {
                    objNode = objNode.Left as Beach;
                }
                else
                {
                    double dblDXR = objSite.x - this.RightBreakPoint(objNode, objSite.y);

                    // x greaterThanWithEpsilon xr => falls somewhere after the right edge of the beachsection
                    if (dblDXR > Polygons.Epsilon)
                    {
                        if (objNode.Right == null)
                        {
                            objLeftArc = objNode;
                            break;
                        }
                        objNode = objNode.Right as Beach;
                    }
                    else
                    {
                        // x equalWithEpsilon xl => falls exactly on the left edge of the beachsection
                        if (dblDXL > -Polygons.Epsilon)
                        {
                            objLeftArc = objNode.Previous as Beach;
                            objRightArc = objNode;
                        }
                        // x equalWithEpsilon xr => falls exactly on the right edge of the beachsection
                        else if (dblDXR > -Polygons.Epsilon)
                        {
                            objLeftArc = objNode;
                            objRightArc = objNode.Next as Beach;
                        }
                        // falls exactly somewhere in the middle of the beachsection
                        else
                        {
                            objLeftArc = objNode;
                            objRightArc = objNode;
                        }

                        break;
                    }
                }
            }

            // At this point, keep in mind that lArc and/or rArc could be null.

            // Create a new beach section object for the site and add it to RBTree.
            Beach objNewArc = this.CreateBeach(objSite);
            this.BeachLines.InsertSuccessor(objLeftArc, objNewArc);

            // [null,null]
            // least likely case: new beach section is the first beach section on the
            // beachline.
            // This case means:
            //   no new transition appears
            //   no collapsing beach section
            //   new beachsection become root of the RB-tree
            if (objLeftArc == null && objRightArc == null)
            {
                return;
            }

            // [lArc,rArc] where lArc == rArc
            // most likely case: new beach section split an existing beach
            // section.
            // This case means:
            //   one new transition appears
            //   the left and right beach section might be collapsing as a result
            //   two new nodes added to the RB-tree
            if (objLeftArc == objRightArc)
            {
                // Invalidate circle event of split beach section.
                this.DetachCircleEvent(objLeftArc);

                // Split the beach section into two separate beach sections.
                objRightArc = this.CreateBeach(objLeftArc.Site);
                this.BeachLines.InsertSuccessor(objNewArc, objRightArc);

                // Since we have a new trasition between two beach sections, a new edge is born.
                objNewArc.Edge = objRightArc.Edge = this.CreateEdge(objLeftArc.Site, objNewArc.Site, null, null);

                // check whether the left and right beach sections are collapsing
                // and if so create circle events, to be notified when the point of
                // collapse is reached.
                this.AttachCircle(objLeftArc);
                this.AttachCircle(objRightArc);

                return;
            }

            // [lArc,null]
            // even less likely case: new beach section is the *last* beach section
            // on the beachline -- this can happen *only* if *all* the previous beach
            // sections currently on the beachline share the same y value as
            // the new beach section.
            // This case means:
            //   one new transition appears
            //   no collapsing beach section as a result
            //   new beach section become right-most node of the RB-tree
            if (objLeftArc != null && objRightArc == null)
            {
                objNewArc.Edge = this.CreateEdge(objLeftArc.Site, objNewArc.Site, null, null);

                return;
            }

            // [null,rArc]
            // impossible case: because sites are strictly processed from top to bottom;
            // and left to right, which guarantees that there will always be a beach section
            // on the left -- except of course when there are no beach section at all on
            // the beach line, which case was handled above.
            // rhill 2011-06-02: No point testing in non-debug version
            //if (!lArc && rArc) {
            //    throw "Voronoi.addBeachsection(): What is this I don't even";
            //    }
            if (objLeftArc == null && objRightArc != null)
            {
                throw new Exception("This must never appear.");
            }

            // [lArc,rArc] where lArc != rArc
            // somewhat less likely case: new beach section falls *exactly* in between two
            // existing beach sections
            // This case means:
            //   one transition disappears
            //   two new transitions appear
            //   the left and right beach section might be collapsing as a result
            //   only one new node added to the RB-tree
            if (objLeftArc != objRightArc)
            {
                // invalidate circle events of left and right sites
                this.DetachCircleEvent(objLeftArc);
                this.DetachCircleEvent(objRightArc);

                // an existing transition disappears, meaning a vertex is defined at
                // the disappearance point.
                // since the disappearance is caused by the new beachsection, the
                // vertex is at the center of the circumscribed circle of the left;
                // new and right beachsections.
                // http://mathforum.org/library/drmath/view/55002.html
                // Except that I bring the origin at A to simplify
                Point objSiteLeft = objLeftArc.Site;
                double ax = objSiteLeft.x;
                double ay = objSiteLeft.y;
                double bx = objSite.x - ax;
                double by = objSite.y - ay;

                Point objSiteRight = objRightArc.Site;
                double cx = objSiteRight.x - ax;
                double cy = objSiteRight.y - ay;
                double d = 2 * (bx * cy - by * cx);
                double hb = bx * bx + by * by;
                double hc = cx * cx + cy * cy;
                Point objVertex = new Point()
                {
                    x = (cy * hb - by * hc) / d + ax,
                    y = (bx * hc - cx * hb) / d + ay
                };

                // One transition disappears.
                objRightArc.Edge.SetStartPoint(objSiteLeft, objSiteRight, objVertex);

                // Two new transitions appear at the new vertex location.
                objNewArc.Edge = this.CreateEdge(objSiteLeft, objSite, null, objVertex);
                objRightArc.Edge = this.CreateEdge(objSite, objSiteRight, null, objVertex);

                // check whether the left and right beach sections are collapsing
                // and if so create circle events, to handle the point of collapse.
                this.AttachCircle(objLeftArc);
                this.AttachCircle(objRightArc);

                return;
            }
        }

        /// <summary>
        /// LeftBreakPoint
        /// </summary>
        /// <created>Dennis Steinmeijer</created>
        /// <date>2013-07-21</date>
        private double LeftBreakPoint(RBNode objNode, double dblDirectrix)
        {
            // Initialise variables.
            double dblRightX = objNode.Site.x;
            double dblRightY = objNode.Site.y;
            double dblPBY2 = dblRightY - dblDirectrix;

            if (dblPBY2 == 0)
            {
                return dblRightX;
            }

            RBNode objLeftNode = objNode.Previous;
            if (objLeftNode == null)
            {
                return -Polygons.Infinity;
            }

            double dblLeftX = objLeftNode.Site.x;
            double dblLeftY = objLeftNode.Site.y;
            double dblPLBY2 = dblLeftY - dblDirectrix;

            if (dblPLBY2 == 0)
            {
                return dblLeftX;
            }

            double dblHL = dblLeftX - dblRightX;
            double dblABY2 = 1 / dblPBY2 - 1 / dblPLBY2;
            double dblB = dblHL / dblPLBY2;

            if (dblABY2 != 0)
            {
                return (-dblB + Math.Sqrt(dblB * dblB - 2 * dblABY2 * (dblHL * dblHL / (-2 * dblPLBY2) - dblLeftY + dblPLBY2 / 2 + dblRightY - dblPBY2 / 2))) / dblABY2 + dblRightX;
            }

            return (dblRightX + dblLeftX) / 2;
        }

        /// <summary>
        /// RightBreakPoint
        /// </summary>
        /// <created>Dennis Steinmeijer</created>
        /// <date>2013-07-21</date>
        private double RightBreakPoint(RBNode objNode, double dblDirectrix)
        {
            RBNode objRightNode = objNode.Next;

            if (objRightNode != null)
            {
                return this.LeftBreakPoint(objRightNode, dblDirectrix);
            }

            return (objNode.Site.y == dblDirectrix) ? objNode.Site.x : Polygons.Infinity;
        }

        /// <summary>
        /// Create a new beach section.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-21</date>
        private Beach CreateBeach(Point objSite)
        {
            // Initialise variables.
            Beach objBeachSection = this.GetBeach();

            // Set the site to the beach section.
            objBeachSection.Site = objSite;

            // Return the beach section.
            return objBeachSection;
        }

        /// <summary>
        /// Detach a circle event.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-21</date>
        private void DetachCircleEvent(Beach objArc)
        {
            Circle objCircle = objArc.Circle;

            if (objCircle == null)
            {
                return;
            }

            if (objCircle.Previous == null)
            {
                this.FirstCircle = objCircle.Next as Circle;
            }

            // Remove from RBTree.
            this.Circles.RemoveNode(objCircle);
            this.CircleJunkyard.Push(objCircle);
            objArc.Circle = null;
        }

        /// <summary>
        /// Creates a new edge.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-21</date>
        private Edge CreateEdge(Point objSiteLeft, Point objSiteRight, Point objVertexA, Point objVertexB)
        {
            Edge objEdge = new Edge()
            {
                SiteLeft = objSiteLeft,
                SiteRight = objSiteRight
            };

            this.Edges.Add(objEdge);

            if (objVertexA != null)
            {
                objEdge.SetStartPoint(objSiteLeft, objSiteRight, objVertexA);
            }

            if (objVertexB != null)
            {
                objEdge.SetEndPoint(objSiteLeft, objSiteRight, objVertexB);
            }

            // Let's create the two half edges between the left and right site.
            this.Cells[objSiteLeft.ID].HalfEdges.Add(new HalfEdge(objEdge, objSiteLeft, objSiteRight));
            this.Cells[objSiteRight.ID].HalfEdges.Add(new HalfEdge(objEdge, objSiteRight, objSiteLeft));

            return objEdge;
        }

        /// <summary>
        /// Attach a circle event.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-22</date>
        private void AttachCircle(Beach objArc)
        {
            // This is a node in the RBTree which points to a beachsection.
            Beach objArcLeft = objArc.Previous as Beach;
            Beach objArcRight = objArc.Next as Beach;

            if (objArcLeft == null || objArcRight == null)
            {
                return;
            } // Does that ever happen?

            Point objSiteLeft = objArcLeft.Site;
            Point objSite = objArc.Site;
            Point objSiteRight = objArcRight.Site;

            // If site of left beachsection is same as site of right beachsection, there can't be convergence.
            if (objSiteLeft == objSiteRight)
            {
                return;
            }

            // Find the circumscribed circle for the three sites associated
            // with the beachsection triplet.
            // rhill 2011-05-26: It is more efficient to calculate in-place
            // rather than getting the resulting circumscribed circle from an
            // object returned by calling Voronoi.circumcircle()
            // http://mathforum.org/library/drmath/view/55002.html
            // Except that I bring the origin at cSite to simplify calculations.
            // The bottom-most part of the circumcircle is our Fortune 'circle
            // event', and its center is a vertex potentially part of the final
            // Voronoi diagram.
            double bx = objSite.x;
            double by = objSite.y;
            double ax = objSiteLeft.x - bx;
            double ay = objSiteLeft.y - by;
            double cx = objSiteRight.x - bx;
            double cy = objSiteRight.y - by;

            // If points l->c->r are clockwise, then center beach section does not
            // collapse, hence it can't end up as a vertex (we reuse 'd' here, which
            // sign is reverse of the orientation, hence we reverse the test.
            // http://en.wikipedia.org/wiki/Curve_orientation#Orientation_of_a_simple_polygon
            // rhill 2011-05-21: Nasty finite precision error which caused circumcircle() to
            // return infinites: 1e-12 seems to fix the problem.
            double d = 2 * (ax * cy - ay * cx);
            if (d >= -2e-12)
            {
                return;
            }

            double ha = ax * ax + ay * ay;
            double hc = cx * cx + cy * cy;
            double x = (cy * ha - ay * hc) / d;
            double y = (ax * hc - cx * ha) / d;
            double ycenter = y + by;

            // Important: ybottom should always be under or at sweep, so no need
            // to waste CPU cycles by checking

            // Recycle circle event object if possible.
            Circle objCircle = this.GetCircle();

            objCircle.Arc = objArc;
            objCircle.Site = objSite;
            objCircle.x = x + bx;
            objCircle.y = ycenter + Math.Sqrt(x * x + y * y); // y bottom
            objCircle.ycenter = ycenter;
            objArc.Circle = objCircle;

            // find insertion point in RB-tree: circle events are ordered from
            // smallest to largest
            Circle objNodePrevious = null;
            Circle objNode = this.Circles.Root as Circle;

            while (objNode != null)
            {
                if (objCircle.y < objNode.y || (objCircle.y == objNode.y && objCircle.x <= objNode.x))
                {
                    if (objNode.Left != null)
                    {
                        objNode = objNode.Left as Circle;
                    }
                    else
                    {
                        objNodePrevious = objNode.Previous as Circle;
                        break;
                    }
                }
                else
                {
                    if (objNode.Right != null)
                    {
                        objNode = objNode.Right as Circle;
                    }
                    else
                    {
                        objNodePrevious = objNode;
                        break;
                    }
                }
            }

            this.Circles.InsertSuccessor(objNodePrevious, objCircle);
            if (objNodePrevious == null)
            {
                this.FirstCircle = objCircle;
            }
        }

        /// <summary>
        /// Checks to see if a circle event is available in the junkyard and uses it,
        /// or makes a new one and returns it.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-22</date>
        private Circle GetCircle()
        {
            Circle objCircle = null;

            // Check to see whether the stack is empty.
            if (this.CircleJunkyard.Count > 0)
            {
                // Try to retrieve a circle vent from the junkyard.
                objCircle = this.CircleJunkyard.Pop();
            }

            // If we don't find a circle event in the junkyard we'll make a new one.
            if (objCircle == null)
            {
                objCircle = new Circle();
            }

            // And give it back!
            return objCircle;
        }

        /// <summary>
        /// Checks to see if a beach section is available in the junkyard and uses it,
        /// or makes a new one and returns it.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-22</date>
        private Beach GetBeach()
        {
            Beach objBeach = null;

            // Check to see whether the stack is empty.
            if (this.BeachJunkyard.Count > 0)
            {
                // Try to retrieve a circle vent from the junkyard.
                objBeach = this.BeachJunkyard.Pop();
            }

            // If we don't find a circle event in the junkyard we'll make a new one.
            if (objBeach == null)
            {
                objBeach = new Beach();
            }

            // And give it back!
            return objBeach;
        }

        /// <summary>
        /// Remove beach section.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-22</date>
        private void RemoveBeach(Beach objBeach)
        {
            Circle objCircle = objBeach.Circle;
            double x = objCircle.x;
            double y = objCircle.ycenter;
            Point objVertex = this.CreateEdgeVertex(x, y);
            Beach objPrevious = objBeach.Previous as Beach;
            Beach objNext = objBeach.Next as Beach;
            var lstDisappearingTransitions = new Transitions();
            lstDisappearingTransitions.AddFirst(objBeach);

            // Remove collapsed beach section from beach line.
            this.DetachBeach(objBeach);

            // there could be more than one empty arc at the deletion point, this
            // happens when more than two edges are linked by the same vertex;
            // so we will collect all those edges by looking up both sides of
            // the deletion point.
            // by the way, there is *always* a predecessor/successor to any collapsed
            // beach section, it's just impossible to have a collapsing first/last
            // beach sections on the beachline, since they obviously are unconstrained
            // on their left/right side.

            // look left
            Beach objLeftArc = objPrevious;
            while (objLeftArc.Circle != null && Math.Abs(x - objLeftArc.Circle.x) < Polygons.Epsilon && Math.Abs(y - objLeftArc.Circle.ycenter) < Polygons.Epsilon)
            {
                objPrevious = objLeftArc.Previous as Beach;
                lstDisappearingTransitions.AddFirst(objLeftArc);
                this.DetachBeach(objLeftArc);
                objLeftArc = objPrevious;
            }

            // even though it is not disappearing, I will also add the beach section
            // immediately to the left of the left-most collapsed beach section, for
            // convenience, since we need to refer to it later as this beach section
            // is the 'left' site of an edge for which a start point is set.
            lstDisappearingTransitions.AddFirst(objLeftArc);
            this.DetachCircleEvent(objLeftArc);

            // look right
            Beach objRightArc = objNext;
            while (objRightArc.Circle != null && Math.Abs(x - objRightArc.Circle.x) < Polygons.Epsilon && Math.Abs(y - objRightArc.Circle.ycenter) < Polygons.Epsilon)
            {
                objNext = objRightArc.Next as Beach;
                lstDisappearingTransitions.AddLast(objRightArc);
                this.DetachBeach(objRightArc);
                objRightArc = objNext;
            }

            // we also have to add the beach section immediately to the right of the
            // right-most collapsed beach section, since there is also a disappearing
            // transition representing an edge's start point on its left.
            lstDisappearingTransitions.AddLast(objRightArc);
            this.DetachCircleEvent(objRightArc);

            // walk through all the disappearing transitions between beach sections and
            // set the start point of their (implied) edge.
            int intArcs = lstDisappearingTransitions.Count;

            for (int intArc = 1; intArc < intArcs; intArc++)
            {
                // Reset objects.
                Beach objRightDis = lstDisappearingTransitions.Item(intArc);
                Beach objLeftDis = lstDisappearingTransitions.Item(intArc - 1);

                if (objRightDis.Edge != null)
                {
                    objRightDis.Edge.SetStartPoint(objLeftDis.Site, objRightDis.Site, objVertex);
                }
            }

            // create a new edge as we have now a new transition between
            // two beach sections which were previously not adjacent.
            // since this edge appears as a new vertex is defined, the vertex
            // actually define an end point of the edge (relative to the site
            // on the left)
            Beach objFirstDis = lstDisappearingTransitions.Item(0);
            Beach objLastDis = lstDisappearingTransitions.Item(intArcs - 1);
            objLastDis.Edge = this.CreateEdge(objFirstDis.Site, objLastDis.Site, null, objVertex);

            // create circle events if any for beach sections left in the beachline
            // adjacent to collapsed sections
            this.AttachCircle(objFirstDis);
            this.AttachCircle(objLastDis);
        }

        /// <summary>
        /// Detach a beach section.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-22</date>
        private void DetachBeach(Beach objBeach)
        {
            // Detach potentially attached circle event.
            this.DetachCircleEvent(objBeach);

            // Remove from RB-tree.
            this.BeachLines.RemoveNode(objBeach);

            // Mark for reuse.
            this.BeachJunkyard.Push(objBeach);
        }

        /// <summary>
        /// Clips all the unnecessary edges.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-23</date>
        private void ClipEdges()
        {
            // Initialise variables.
            int intEdges = this.Edges.Count - 1;

            while (intEdges >= 0)
            {
                // Edge is removed if:
                // - it is wholly outside the bounding box.
                // - it is actually a point rather than a line.
                Edge objEdge = this.Edges[intEdges];
                bool blnRemove = false;

                if (!this.ConnectEdge(objEdge))
                {
                    blnRemove = true;
                }

                if (!blnRemove && !this.ClipEdge(objEdge))
                {
                    blnRemove = true;
                }

                // Is the edge a point rather than a line?
                if (!blnRemove && (this.LessThanEpsilonAbs(objEdge.VertexA.x, objEdge.VertexB.x) && this.LessThanEpsilonAbs(objEdge.VertexA.y, objEdge.VertexB.y)))
                {
                    blnRemove = true;
                }

                // De we need to remove the edge entirely?
                if (blnRemove)
                {
                    objEdge.VertexA = null;
                    objEdge.VertexB = null;

                    // Remove from collection.
                    this.Edges.RemoveAt(intEdges);
                }

                // Lower the edge counter.
                intEdges--;
            }
        }

        /// <summary>
        /// Connects an edge.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-23</date>
        private Boolean ConnectEdge(Edge objEdge)
        {
            if (objEdge.VertexB != null)
            {
                return true;
            }

            // Make a local copy for performance purpose.
            double fx = (objEdge.SiteLeft.x + objEdge.SiteRight.x) / 2;
            double fy = (objEdge.SiteLeft.y + objEdge.SiteRight.y) / 2;
            double? fm = null;
            double? fb = null;

            // Get the line equation of the bisector if line is not vertical.
            if ((objEdge.SiteRight.y - objEdge.SiteLeft.y) != 0)
            {
                fm = (objEdge.SiteLeft.x - objEdge.SiteRight.x) / (objEdge.SiteRight.y - objEdge.SiteLeft.y);
                fb = fy - fm * fx;
            }

            // remember, direction of line (relative to left site):
            // upward: left.x < right.x
            // downward: left.x > right.x
            // horizontal: left.x == right.x
            // upward: left.x < right.x
            // rightward: left.y < right.y
            // leftward: left.y > right.y
            // vertical: left.y == right.y

            // depending on the direction, find the best side of the
            // bounding box to use to determine a reasonable start point

            // Special case: vertical line.
            if (fm == null)
            {
                // doesn't intersect with viewport
                if (fx < this.Box.xl || fx >= this.Box.xr)
                {
                    return false;
                }

                // Downward vertical line.
                if (objEdge.SiteLeft.x > objEdge.SiteRight.x)
                {
                    if (objEdge.VertexA == null)
                    {
                        objEdge.VertexA = this.CreateEdgeVertex(fx, this.Box.yt);
                    }
                    else if (objEdge.VertexA.y >= this.Box.yb)
                    {
                        return false;
                    }

                    objEdge.VertexB = this.CreateEdgeVertex(fx, this.Box.yb);
                }
                // Upward vertical line.
                else
                {
                    if (objEdge.VertexA == null)
                    {
                        objEdge.VertexA = this.CreateEdgeVertex(fx, this.Box.yb);
                    }
                    else if (objEdge.VertexA.y < this.Box.yt)
                    {
                        return false;
                    }

                    objEdge.VertexB = this.CreateEdgeVertex(fx, this.Box.yt);
                }
            }
            // Closer to vertical than horizontal, connect start point to the top or bottom side of the bounding box.
            else if (fm < -1 || fm > 1)
            {
                // Downward.
                if (objEdge.SiteLeft.x > objEdge.SiteRight.x)
                {
                    if (objEdge.VertexA == null)
                    {
                        objEdge.VertexA = this.CreateEdgeVertex((this.Box.yt - (double)fb) / (double)fm, this.Box.yt);
                    }
                    else if (objEdge.VertexA.y >= this.Box.yb)
                    {
                        return false;
                    }

                    objEdge.VertexB = this.CreateEdgeVertex((this.Box.yb - (double)fb) / (double)fm, this.Box.yb);
                }
                // Upward.
                else
                {
                    if (objEdge.VertexA == null)
                    {
                        objEdge.VertexA = this.CreateEdgeVertex((this.Box.yb - (double)fb) / (double)fm, this.Box.yb);
                    }
                    else if (objEdge.VertexA.y < this.Box.yt)
                    {
                        return false;
                    }

                    objEdge.VertexB = this.CreateEdgeVertex((this.Box.yt - (double)fb) / (double)fm, this.Box.yt);
                }
            }
            // Closer to horizontal than vertical, connect start point to the left or right side of the bounding box.
            else
            {
                // Rightward.
                if (objEdge.SiteLeft.y < objEdge.SiteRight.y)
                {
                    if (objEdge.VertexA == null)
                    {
                        objEdge.VertexA = this.CreateEdgeVertex(this.Box.xl, (double)fm * this.Box.xl + (double)fb);
                    }
                    else if (objEdge.VertexA.x >= this.Box.xr)
                    {
                        return false;
                    }

                    objEdge.VertexB = this.CreateEdgeVertex(this.Box.xr, (double)fm * this.Box.xr + (double)fb);
                }
                // Leftward.
                else
                {
                    if (objEdge.VertexA == null)
                    {
                        objEdge.VertexA = this.CreateEdgeVertex(this.Box.xr, (double)fm * this.Box.xr + (double)fb);
                    }
                    else if (objEdge.VertexA.x < this.Box.xl)
                    {
                        return false;
                    }

                    objEdge.VertexB = this.CreateEdgeVertex(this.Box.xl, (double)fm * this.Box.xl + (double)fb);
                }
            }

            // If we end up here, it means the edge is an outer edge.
            objEdge.Type = Enumerations.EdgeType.Outer;

            return true;
        }

        /// <summary>
        /// Clip an edge to fit the bounding box.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-23</date>
        private Boolean ClipEdge(Edge objEdge)
        {
            // Initialise variables.
            double ax = objEdge.VertexA.x;
            double ay = objEdge.VertexA.y;
            double bx = objEdge.VertexB.x;
            double by = objEdge.VertexB.y;
            double t0 = 0;
            double t1 = 1;
            double dblDeltaX = bx - ax;
            double dblDeltaY = by - ay;

            // Left.
            double q = ax - this.Box.xl;
            if (dblDeltaX == 0 && q < 0)
            {
                return false;
            }

            if (dblDeltaX != 0)
            {
                double r = -q / dblDeltaX;

                if (dblDeltaX < 0)
                {
                    if (r < t0)
                    {
                        return false;
                    }
                    else if (r < t1)
                    {
                        t1 = r;
                    }
                }
                else if (dblDeltaX > 0)
                {
                    if (r > t1)
                    {
                        return false;
                    }
                    else if (r > t0)
                    {
                        t0 = r;
                    }
                }
            }

            // Right.
            q = this.Box.xr - ax;
            if (dblDeltaX == 0 && q < 0)
            {
                return false;
            }

            if (dblDeltaX != 0)
            {
                double r = q / dblDeltaX;

                if (dblDeltaX < 0)
                {
                    if (r > t1)
                    {
                        return false;
                    }
                    else if (r > t0)
                    {
                        t0 = r;
                    }
                }
                else if (dblDeltaX > 0)
                {
                    if (r < t0)
                    {
                        return false;
                    }
                    else if (r < t1)
                    {
                        t1 = r;
                    }
                }
            }

            // Top.
            q = ay - this.Box.yt;
            if (dblDeltaY == 0 && q < 0)
            {
                return false;
            }

            if (dblDeltaY != 0)
            {
                double r = -q / dblDeltaY;

                if (dblDeltaY < 0)
                {
                    if (r < t0)
                    {
                        return false;
                    }
                    else if (r < t1)
                    {
                        t1 = r;
                    }
                }
                else if (dblDeltaY > 0)
                {
                    if (r > t1)
                    {
                        return false;
                    }
                    else if (r > t0)
                    {
                        t0 = r;
                    }
                }
            }

            // Bottom.
            q = this.Box.yb - ay;
            if (dblDeltaY == 0 && q < 0)
            {
                return false;
            }

            if (dblDeltaY != 0)
            {
                double r = q / dblDeltaY;

                if (dblDeltaY < 0)
                {
                    if (r > t1)
                    {
                        return false;
                    }
                    else if (r > t0)
                    {
                        t0 = r;
                    }
                }
                else if (dblDeltaY > 0)
                {
                    if (r < t0)
                    {
                        return false;
                    }
                    else if (r < t1)
                    {
                        t1 = r;
                    }
                }
            }

            // If we reach this point, the Voronoi edge is within the bounding box.

            // if t0 > 0, va needs to change
            // rhill 2011-06-03: we need to create a new vertex rather
            // than modifying the existing one, since the existing
            // one is likely shared with at least another edge
            if (t0 > 0)
            {
                objEdge.VertexA = this.CreateEdgeVertex(ax + t0 * dblDeltaX, ay + t0 * dblDeltaY);

                // So if we need to clip the edge, it means it's an outer edge.
                objEdge.Type = Enumerations.EdgeType.Outer;
            }

            // if t1 < 1, vb needs to change
            // rhill 2011-06-03: we need to create a new vertex rather
            // than modifying the existing one, since the existing
            // one is likely shared with at least another edge
            if (t1 < 1)
            {
                objEdge.VertexB = this.CreateEdgeVertex(ax + t1 * dblDeltaX, ay + t1 * dblDeltaY);

                // So if we need to clip the edge, it means it's an outer edge.
                objEdge.Type = Enumerations.EdgeType.Outer;
            }

            return true;
        }

        /// <summary>
        /// Close cells.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-13</date>
        private void CloseCells()
        {
            // Prune and order the halfedges, then add missing halfedges in order to close the cells.
            for (int intCell = this.Cells.Count; intCell > 0; intCell--)
            {
                // Grab the cell object.
                Cell objCell = this.Cells[intCell];

                // Prepare the cell by pruning and ordering the halfedges.
                objCell.PrepareHalfEdges();

                // close open cells
                // step 1: find first 'unclosed' point, if any.
                // an 'unclosed' point will be the end point of a halfedge which
                // does not match the start point of the following halfedge
                int intHalfEdges = objCell.HalfEdges.Count;
                int intLeft = 0;

                while (intLeft < intHalfEdges)
                {
                    int intRight = (intLeft + 1) % intHalfEdges;
                    Point objEnd = objCell.HalfEdges[intLeft].GetEndPoint();
                    Point objStart = objCell.HalfEdges[intRight].GetStartPoint();

                    // if end point is not equal to start point, we need to add the missing halfedge(s) to close the cell
                    if ((Math.Abs(objEnd.x - objStart.x) >= Polygons.Epsilon || Math.Abs(objEnd.y - objStart.y) >= Polygons.Epsilon))
                    {
                        // if we reach this point, cell needs to be closed by walking counterclockwise along the bounding box until it connects to next halfedge in the list
                        Point objVertexA = objEnd;
                        double x;
                        double y;
                        Point objVertexB = null;

                        // walk downward along left side
                        if (this.EqualWithEpsilon(objEnd.x, this.Box.xl) && this.LessThanWithEpsilon(objEnd.y, this.Box.yb))
                        {
                            x = this.Box.xl;
                            y = this.EqualWithEpsilon(objStart.x, this.Box.xl) ? objStart.y : this.Box.yb;
                            objVertexB = this.CreateEdgeVertex(x, y);
                        }
                        // walk rightward along bottom side
                        else if (this.EqualWithEpsilon(objEnd.y, this.Box.yb) && this.LessThanWithEpsilon(objEnd.x, this.Box.xr))
                        {
                            x = this.EqualWithEpsilon(objStart.y, this.Box.yb) ? objStart.x : this.Box.xr;
                            y = this.Box.yb;
                            objVertexB = this.CreateEdgeVertex(x, y);
                        }
                        // walk upward along right side
                        else if (this.EqualWithEpsilon(objEnd.x, this.Box.xr) && this.GreaterThanWithEpsilon(objEnd.y, this.Box.yt))
                        {
                            x = this.Box.xr;
                            y = this.EqualWithEpsilon(objStart.x, this.Box.xr) ? objStart.y : this.Box.yt;
                            objVertexB = this.CreateEdgeVertex(x, y);
                        }
                        // walk leftward along top side
                        else if (this.EqualWithEpsilon(objEnd.y, this.Box.yt) && this.GreaterThanWithEpsilon(objEnd.x, this.Box.xl))
                        {
                            x = this.EqualWithEpsilon(objStart.y, this.Box.yt) ? objStart.x : this.Box.xl;
                            y = this.Box.yt;
                            objVertexB = this.CreateEdgeVertex(x, y);
                        }

                        // Create a new half edge.
                        HalfEdge objHalfEdge = new HalfEdge(this.CreateBorderEdge(objCell.Site, objVertexA, objVertexB), objCell.Site, null);

                        objCell.HalfEdges.Insert(intLeft + 1, objHalfEdge);

                        intHalfEdges = objCell.HalfEdges.Count;
                    }

                    if (intLeft > 100)
                    {
                        // Clearly something is going awfully wrong. What site is causing this problem?
                        throw new Exception("CloseCells: Trouble with seed " + this.Seed + ". Site (" + objCell.Site.ID.ToString() + ") " + objCell.Site.ToString() + ".");
                    }

                    intLeft++;
                }
            }
        }

        /// <summary>
        /// Creates a border edge.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-23</date>
        private Edge CreateBorderEdge(Point objSite, Point objVertexA, Point objVertexB)
        {
            Edge objEdge = new Edge()
            {
                SiteLeft = objSite,
                SiteRight = null,
                VertexA = objVertexA,
                VertexB = objVertexB,
                Type = Enumerations.EdgeType.Outer
            };

            this.Edges.Add(objEdge);

            return objEdge;
        }

        /// <summary>
        /// Creates a vertex for an edge.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-08-15</date>
        private Point CreateEdgeVertex(double dblX, double dblY)
        {
            // Initialise variable.
            Point objPoint;
            string strCoordinates = string.Format("{0}x{1}", dblX, dblY);

            if (this.EdgeVertices.ContainsKey(strCoordinates))
            {
                objPoint = this.EdgeVertices[strCoordinates];
            }
            else
            {
                objPoint = new Point() { x = dblX, y = dblY };

                this.EdgeVertices.Add(strCoordinates, objPoint);
            }

            return objPoint;
        }
    }
}
