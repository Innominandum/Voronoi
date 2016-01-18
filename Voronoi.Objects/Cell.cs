using System;
using System.Linq;

namespace Voronoi
{
    public class Cell : RBNode
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

        public System.Collections.Generic.List<HalfEdge> HalfEdges = new System.Collections.Generic.List<HalfEdge>();
        public System.Collections.Generic.List<Point> Points = new System.Collections.Generic.List<Point>();
        public System.Collections.Generic.Dictionary<Edge, Cell> Neighbours = new System.Collections.Generic.Dictionary<Edge, Cell>();
        public Enumerations.ElevationZones ElevationZone;
        public double ElevationIndex = -1;

        public bool IsOuterEdge
        {
            get
            {
                foreach (HalfEdge objHalfEdge in this.HalfEdges)
                {
                    if (objHalfEdge.Edge.Type == Enumerations.EdgeType.Outer)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private string _PolygonPoints;
        public string PolygonPoints
        {
            get
            {
                if (string.IsNullOrEmpty(this._PolygonPoints))
                {
                    // Check to see if the points have been collected yet.
                    if (this.Points.Count == 0)
                    {
                        // Collect the points.
                        this.DeterminePolygonPoints();
                    }

                    // Now we determine the polygon's points, represented as a string, usuable in the svg.
                    this._PolygonPoints = string.Join(" ", this.Points.Select(p => p.x.ToString().Replace(",", ".") + "," + p.y.ToString().Replace(",", ".")));
                }

                return this._PolygonPoints;
            }
        }

        private Enumerations.Biomes _Biome;
        public Enumerations.Biomes Biome
        {
            get
            {
                return this._Biome;
            }
            set
            {
                // If the cell is not water or ocean, which means the noise has already been inverted.
                if (this._Biome != Enumerations.Biomes.Ocean && this._Biome != Enumerations.Biomes.Water)
                {
                    // Check to see if the new value is a water or ocean cell. If so, invert the noise.
                    if (value == Enumerations.Biomes.Ocean || value == Enumerations.Biomes.Water)
                    {
                        this.Site.Noise = 1 - this.Site.Noise;
                    }
                }

                this._Biome = value;
            }
        }

        #endregion

        /// <summary>
        /// Trim non fully defined half edges and sort them counter-clockwise.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-23</date>
        public void PrepareHalfEdges()
        {
            try
            {
                // Initialise variables.
                int intHalfEdges = this.HalfEdges.Count - 1;

                // Get rid of unused halfedges. Go bottom up so that when halfedges are removed the index doesn't get fucked up.
                while (intHalfEdges >= 0)
                {
                    // Grab the edge from the halfedge.
                    Edge objEdge = this.HalfEdges[intHalfEdges].Edge;

                    // If the edge is missing either or both of the points, remove it.
                    if (objEdge.VertexB == null || objEdge.VertexA == null) { this.HalfEdges.RemoveAt(intHalfEdges); }

                    // Continue with the next.
                    intHalfEdges--;
                }

                // Sort the halfedges.
                this.HalfEdges.Sort(SortHalfEdges);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in Prepare", ex);
            }
        }

        /// <summary>
        /// Sort the half edges.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-08-08</date>
        private int SortHalfEdges(HalfEdge a, HalfEdge b)
        {
            try
            {
                double r = b.Angle - a.Angle;
                if (r < 0) { return -1; }
                if (r > 0) { return 1; }
                return 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in SortHalfEdge", ex);
            }
        }

        /// <summary>
        /// Traces all the edges and collects the relevation points. What is important to note
        /// is that while the voronoi polygons are determined, the edges are prepared and sorted
        /// in PrepareHalfEdges(). This is important, since it placed the edges in the right
        /// order, determined by their angle.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-08-20</date>
        public void DeterminePolygonPoints()
        {
            try
            {
                // Grab all the points. Start by grabbing the starting point.
                this.Points.Add(this.HalfEdges[0].GetStartPoint());

                // Then get all the subsequent end points.
                foreach (Voronoi.HalfEdge objHalfEdge in this.HalfEdges)
                {
                    this.Points.Add(objHalfEdge.GetEndPoint());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DeterminePolygonPoints", ex);
            }
        }

        /// <summary>
        /// Sets the elevation for the site and each points that make up the cell.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-08-08</date>
        public void DetermineElevation(int intPeak)
        {
            try
            {
                // Initialise variables.
                double dblNoise = this.Site.Noise;

                // By setting the water property, we're essentially determining whether the elevation of each
                // of the half edges is positive or negative. Therefore, we need to set the noise to a positive
                // or negative number for each of the half edges, as well as the site.
                if (this.Biome == Enumerations.Biomes.Water || this.Biome == Enumerations.Biomes.Ocean)
                {
                    dblNoise = 0 - Math.Abs(dblNoise);
                }
                else
                {
                    dblNoise = 0 + Math.Abs(dblNoise);
                }

                // First let's determine the elevation of the site.
                this.Site.z = (dblNoise * intPeak);

                // Now, if this is not a water or ocean cell, we'll need to redistribute the elevation.
                if (this.Biome != Enumerations.Biomes.Water && this.Biome != Enumerations.Biomes.Ocean)
                {
                    this.Site.z *= this.ElevationIndex;
                }

                // Now determine the elevation for each of the points that make up the cell.
                foreach (Voronoi.Point objPoint in this.Points)
                {
                    double dblPointNoise = objPoint.Noise;

                    if (this.Biome == Enumerations.Biomes.Water || this.Biome == Enumerations.Biomes.Ocean)
                    {
                        dblPointNoise = 0 - Math.Abs(dblPointNoise);
                    }
                    else
                    {
                        dblPointNoise = 0 + Math.Abs(dblPointNoise);
                    }

                    objPoint.z = (dblPointNoise * intPeak);

                    // Now, if this is not a water or ocean cell, we'll need to redistribute the elevation.
                    if (this.Biome != Enumerations.Biomes.Water && this.Biome != Enumerations.Biomes.Ocean)
                    {
                        objPoint.z *= this.ElevationIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DetermineElevation", ex);
            }
        }
    }
}
