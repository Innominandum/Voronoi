using System;

namespace Voronoi
{
    public class Edge
    {
        #region Properties

        public Point SiteLeft;
        public Point SiteRight;
        public Enumerations.EdgeType Type = Enumerations.EdgeType.None;
        public Int16 River = 0;

        private Point _VertexA;
        public Point VertexA
        {
            get
            {
                return this._VertexA;
            }
            set
            {
                // If the value is set to null, we'll need to remove the point
                // from the neighbours of the opposite vertex on the edge. If
                // it's an actual point, then let's add it to the neighbours
                // of the opposite vertex on the edge.
                if (value == null)
                {
                    // If the opposite vertex is a point, then we'll remove the
                    // point from the neighbours of the opposite point.
                    if (this._VertexB != null)
                    {
                        if (this._VertexB.Neighbours.ContainsKey(this))
                        {
                            this._VertexB.Neighbours.Remove(this);
                        }
                    }
                }
                else
                {
                    // If the opposite vertex is a point, we'll add the value
                    // as one of its neighbours, and vice versa.
                    if (this._VertexB != null)
                    {
                        if (this._VertexB.Neighbours.ContainsKey(this))
                        {
                            this._VertexB.Neighbours[this] = value;
                        }
                        else
                        {
                            this._VertexB.Neighbours.Add(this, value);
                        }

                        if (value.Neighbours.ContainsKey(this))
                        {
                            value.Neighbours[this] = this._VertexB;
                        }
                        else
                        {
                            value.Neighbours.Add(this, this._VertexB);
                        }
                    }
                }

                // Set the value to the vertex.
                this._VertexA = value;
            }
        }

        private Point _VertexB;
        public Point VertexB
        {
            get
            {
                return this._VertexB;
            }
            set
            {
                // If the value is set to null, we'll need to remove the point
                // from the neighbours of the opposite vertex on the edge. If
                // it's an actual point, then let's add it to the neighbours
                // of the opposite vertex on the edge.
                if (value == null)
                {
                    // If the opposite vertex is a point, then we'll remove the
                    // point from the neighbours of the opposite point.
                    if (this._VertexA != null)
                    {
                        if (this._VertexA.Neighbours.ContainsKey(this))
                        {
                            this._VertexA.Neighbours.Remove(this);
                        }
                    }
                }
                else
                {
                    // If the opposite vertex is a point, we'll add the value
                    // as one of its neighbours, and vice versa.
                    if (this._VertexA != null)
                    {
                        if (this._VertexA.Neighbours.ContainsKey(this))
                        {
                            this._VertexA.Neighbours[this] = value;
                        }
                        else
                        {
                            this._VertexA.Neighbours.Add(this, value);
                        }

                        if (value.Neighbours.ContainsKey(this))
                        {
                            value.Neighbours[this] = this._VertexA;
                        }
                        else
                        {
                            value.Neighbours.Add(this, this._VertexA);
                        }
                    }
                }

                // Set the value to the vertex.
                this._VertexB = value;
            }
        }

        #endregion

        /// <summary>
        /// Set the start point of the edge.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-22</date>
        public void SetStartPoint(Point objSiteLeft, Point objSiteRight, Point objVertex)
        {
            try
            {
                if (this.VertexA == null && this.VertexB == null)
                {
                    this.VertexA = objVertex;
                    this.SiteLeft = objSiteLeft;
                    this.SiteRight = objSiteRight;
                }
                else 
                {
                    if (this.SiteLeft == objSiteRight)
                    {
                        this.VertexB = objVertex;
                    }
                    else
                    {
                        this.VertexA = objVertex;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in SetStartPoint", ex);
            }
        }

        /// <summary>
        /// Set end point.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-22</date>
        public void SetEndPoint(Point objSiteLeft, Point objSiteRight, Point objVertex)
        {
            try
            {
                this.SetStartPoint(objSiteRight, objSiteLeft, objVertex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in SetEndPoint", ex);
            }   
        }
    }
}
