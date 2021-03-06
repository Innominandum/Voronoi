﻿using System;

namespace Voronoi.Objects
{
    public class HalfEdge
    {
        #region Properties

        public Edge Edge;
        public Point Site;
        public double Angle;

        #endregion

        #region Constructor

        /// <summary>
        /// Class constructor.
        /// </summary>
        
        /// <date>2013-07-23</date>
        public HalfEdge(Edge objEdge, Point objSiteLeft, Point objSiteRight)
        {
            this.Site = objSiteLeft;
            this.Edge = objEdge;

            // 'angle' is a value to be used for properly sorting the
            // halfsegments counterclockwise. By convention, we will
            // use the angle of the line defined by the 'site to the left'
            // to the 'site to the right'.
            // However, border edges have no 'site to the right': thus we
            // use the angle of line perpendicular to the halfsegment (the
            // edge should have both end points defined in such case.)
            if (objSiteRight != null)
            {
                this.Angle = Math.Atan2(objSiteRight.y - objSiteLeft.y, objSiteRight.x - objSiteLeft.x);
            }
            else
            {
                if (objEdge.SiteLeft == objSiteLeft)
                {
                    this.Angle = Math.Atan2(objEdge.VertexB.x - objEdge.VertexA.x, objEdge.VertexA.y - objEdge.VertexB.y);
                }
                else
                {
                    this.Angle = Math.Atan2(objEdge.VertexA.x - objEdge.VertexB.x, objEdge.VertexB.y - objEdge.VertexA.y);
                }
            }
        }

        #endregion

        /// <summary>
        /// Get the start point.
        /// </summary>
        public Point GetStartPoint()
        {
            if (this.Edge.SiteLeft == this.Site)
            {
                return this.Edge.VertexA;
            }
            else
            {
                return this.Edge.VertexB;
            }
        }

        /// <summary>
        /// Get the end point.
        /// </summary>
        public Point GetEndPoint()
        {
            if (this.Edge.SiteLeft == this.Site)
            {
                return this.Edge.VertexB;
            }
            else
            {
                return this.Edge.VertexA;
            }
        }
    }
}
