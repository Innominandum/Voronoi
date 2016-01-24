using System;
using System.Collections.Generic;
using System.Linq;
using Voronoi.Objects.Enumerations;

namespace Voronoi.Objects
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

        public List<HalfEdge> HalfEdges = new List<HalfEdge>();
        public List<Point> Points = new List<Point>();
        public Dictionary<Edge, Cell> Neighbours = new Dictionary<Edge, Cell>();
        public ElevationZones ElevationZone;
        public double ElevationIndex = -1;

        public bool IsOuterEdge
        {
            get
            {
                foreach (HalfEdge objHalfEdge in this.HalfEdges)
                {
                    if (objHalfEdge.Edge.Type == EdgeType.Outer)
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

        private Biomes _Biome;
        public Biomes Biome
        {
            get
            {
                return this._Biome;
            }
            set
            {
                // If the cell is not water or ocean, which means the noise has already been inverted.
                if (this._Biome != Biomes.Ocean && this._Biome != Biomes.Water)
                {
                    // Check to see if the new value is a water or ocean cell. If so, invert the noise.
                    if (value == Biomes.Ocean || value == Biomes.Water)
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
        public void PrepareHalfEdges()
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

        /// <summary>
        /// Sort the half edges.
        /// </summary>
        private int SortHalfEdges(HalfEdge a, HalfEdge b)
        {
            double r = b.Angle - a.Angle;
            if (r < 0) { return -1; }
            if (r > 0) { return 1; }
            return 0;
        }

        /// <summary>
        /// Traces all the edges and collects the relevation points. What is important to note
        /// is that while the voronoi polygons are determined, the edges are prepared and sorted
        /// in PrepareHalfEdges(). This is important, since it placed the edges in the right
        /// order, determined by their angle.
        /// </summary>
        public void DeterminePolygonPoints()
        {
            // Grab all the points. Start by grabbing the starting point.
            this.Points.Add(this.HalfEdges[0].GetStartPoint());

            // Then get all the subsequent end points.
            foreach (HalfEdge objHalfEdge in this.HalfEdges)
            {
                this.Points.Add(objHalfEdge.GetEndPoint());
            }
        }

        /// <summary>
        /// Sets the elevation for the site and each points that make up the cell.
        /// </summary>
        public void DetermineElevation(int intPeak)
        {
            // Initialise variables.
            double dblNoise = this.Site.Noise;

            // By setting the water property, we're essentially determining whether the elevation of each
            // of the half edges is positive or negative. Therefore, we need to set the noise to a positive
            // or negative number for each of the half edges, as well as the site.
            if (this.Biome == Biomes.Water || this.Biome == Biomes.Ocean)
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
            if (this.Biome != Biomes.Water && this.Biome != Biomes.Ocean)
            {
                this.Site.z *= this.ElevationIndex;
            }

            // Now determine the elevation for each of the points that make up the cell.
            foreach (Point objPoint in this.Points)
            {
                double dblPointNoise = objPoint.Noise;

                if (this.Biome == Biomes.Water || this.Biome == Biomes.Ocean)
                {
                    dblPointNoise = 0 - Math.Abs(dblPointNoise);
                }
                else
                {
                    dblPointNoise = 0 + Math.Abs(dblPointNoise);
                }

                objPoint.z = (dblPointNoise * intPeak);

                // Now, if this is not a water or ocean cell, we'll need to redistribute the elevation.
                if (this.Biome != Biomes.Water && this.Biome != Biomes.Ocean)
                {
                    objPoint.z *= this.ElevationIndex;
                }
            }
        }
    }
}
