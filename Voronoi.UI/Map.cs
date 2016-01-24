using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Voronoi.Objects;
using Voronoi.Objects.Enumerations;
using Voronoi.UI.Enumerations;
using System.Collections.Generic;

namespace Voronoi.UI
{
    public class Map
    {
        #region Properties

        // Objects.
        private Random Random;
        private Polygons Polygons;
        private Perlin Perlin;
        private Stopwatch Stopwatch = new Stopwatch();

        // Debug.
        //private string Log = @"C:\Users\DV8\Desktop\Projects\Map\Output\log.txt";

        // Output properties.
        public StringBuilder Debug = new StringBuilder();
        public string VectorMap;

        #endregion

        #region Settings

        public int Height { get; set; }
        public int Width { get; set; }
        public int NumberOfSites { get; set; }
        public int PolygonSeed { get; set; }
        public int PerlinSeed { get; set; }
        public int LloydIterations { get; set; }
        public bool ShowBorders { get; set; }
        public MapType MapType { get; set; }
        public int NoiseOctaves { get; set; }

        // Static (for now) settings.
        private int Peak = 8000;
        public MapShape IslandShape;

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public Map()
        {
            try
            {
                // Start the time.
                this.Stopwatch.Start();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in Map", ex);
            }
        }

        /// <summary>
        /// Create the map.
        /// </summary>
        public void Create()
        {
            try
            {
                // Log.
                this.Write("Start with Create");

                // Let's initialise some settings.
                this.Initialise();

                // Let's calculate some sites.
                this.CalculateSites();

                // Process all the sites, setting properties for them.
                this.ProcessSites();

                // Collate the map.
                this.DrawMap();

                // Stop the time.
                this.Stopwatch.Stop();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in Create", ex);
            }
            finally
            {
                this.Write("End with Create");
            }
        }

        /// <summary>
        /// Initialise.
        /// </summary>
        private void Initialise()
        {
            try
            {
                // Log.
                this.Write("Start with Initialise");

                // Create a random object based on a supplied seed.
                this.Random = new Random(this.PolygonSeed);

                // Create a perlin object based on a supplied seed.
                this.Perlin = new Perlin(this.PerlinSeed);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in Initialise", ex);
            }
            finally
            {
                this.Write("End with Initialise");
            }
        }

        /// <summary>
        /// Calculates and initiates new sites.
        /// </summary>
        private void CalculateSites()
        {
            try
            {
                // Log.
                this.Write("CalculateSites: start with run 0");

                // Create a new voronoi object.
                this.Polygons = new Polygons()
                {
                    Height = this.Height,
                    Width = this.Width,
                    Seed = this.PolygonSeed
                };

                // Create a bunch of random point objects and add them to the list of points.
                for (int intCount = 0; intCount < this.NumberOfSites; intCount++)
                {
                    this.Polygons.SiteList.Add(new Point { x = this.Random.Next(0, this.Width), y = this.Random.Next(0, this.Height) });
                }

                // Let's grab the polygons.
                this.Polygons.Compute();

                // Log.
                this.Write("CalculateSites: done with run 0");

                // Let's add some blue noise.
                for (int intImprove = 0; intImprove < this.LloydIterations; intImprove++)
                {
                    // Log.
                    this.Write(string.Format("CalculateSites: start with run {0}", intImprove + 1));

                    // Initialise the sites list.
                    var lstSites = new List<Point>();

                    // Go through each polygon.
                    foreach (KeyValuePair<int, Cell> kvpCell in this.Polygons.Cells)
                    {
                        // Initialise some variables.
                        double px = 0;
                        double py = 0;

                        // Grab all the points involved in the polygon and add the x and y coordinates together.
                        for (int intHalfEdge = 0; intHalfEdge < kvpCell.Value.HalfEdges.Count; intHalfEdge++)
                        {
                            HalfEdge currentHalfEdge = kvpCell.Value.HalfEdges[intHalfEdge];

                            px += currentHalfEdge.Edge.VertexA.x;
                            py += currentHalfEdge.Edge.VertexA.y;
                            px += currentHalfEdge.Edge.VertexB.x;
                            py += currentHalfEdge.Edge.VertexB.y;
                        }

                        // Make a new point with the average x and average y coordinate for all the points in the polygon.
                        // This will center the voronoi points more and space them more evenly apart.
                        px = (kvpCell.Value.HalfEdges.Count == 0) ? 1 : Math.Round(px / (kvpCell.Value.HalfEdges.Count * 2), 0);
                        py = (kvpCell.Value.HalfEdges.Count == 0) ? 1 : Math.Round(py / (kvpCell.Value.HalfEdges.Count * 2), 0);

                        // Add the new, adjusted point to a new list of sites.
                        lstSites.Add(new Point { x = px, y = py });
                    }

                    // Once we've adjusted all the sites, we'll reset the voronoi object...
                    this.Polygons.Reset();

                    // ...and feed it the new list of points.
                    foreach (Point currentPoint in lstSites)
                    {
                        this.Polygons.SiteList.Add(new Point { x = currentPoint.x, y = currentPoint.y });
                    }

                    // And we'll calculate the polygons again!
                    this.Polygons.Compute();

                    // clear the list.
                    lstSites.Clear();
                    lstSites = null;

                    // Log.
                    this.Write(string.Format("CalculateSites: done with run {0}", intImprove + 1));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in CalculateSites", ex);
            }
        }

        /// <summary>
        /// Processes all the sites and sets properties for them.
        /// </summary>
        private void ProcessSites()
        {
            try
            {
                // Log.
                this.Write("Start with ProcessSites");

                // Run through each cell.
                foreach (KeyValuePair<int, Cell> kvpCell in this.Polygons.Cells)
                {
                    // Grab the cell object.
                    Cell currentCell = kvpCell.Value;

                    // Determine cell's points.
                    currentCell.DeterminePolygonPoints();

                    // Determine cell's neighbours.
                    this.DetermineCellNeighbours(currentCell);

                    // Determines the noise of each cell and all its points.
                    this.DetermineCellNoise(currentCell);
                }

                // Determine the island shape and see whether each cell consists of land or water.
                switch (this.IslandShape)
                {
                    case MapShape.Radial:
                        this.IslandTypeRadial();
                        break;
                    case MapShape.Perlin:
                        this.IslandTypePerlin();
                        break;
                    default:
                        throw new NotImplementedException(string.Format("This island shape has not been implemented yet: {0}.", this.IslandShape));
                }

                // Now we know what's water and what isn't, let's determine what's ocean.
                this.DetermineOcean();

                // We know what is land, what is water, and what is ocean. Now let's mark the coastline.
                this.DetermineCoast();

                // Determine elevation or depth for each cell.
                this.DetermineCellElevation();

                // Determine rivers.
                this.DetermineRivers();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in ProcessSites", ex);
            }
            finally
            {
                this.Write("End with ProcessSites");
            }
        }

        /// <summary>
        /// Determine the neighbours of each cell.
        /// </summary>
        private void DetermineCellNeighbours(Cell currentCell)
        {
            try
            {
                // Then run through each of the cell's edges.
                foreach (HalfEdge currentHalfEdge in currentCell.HalfEdges)
                {
                    // Now grab the edge.
                    Edge currentEdge = currentHalfEdge.Edge;

                    // Check to see whether the cell is the left or the right side of the edge.
                    if (currentCell.Site == currentEdge.SiteLeft)
                    {
                        // The cell is left, let's grab right. When we do, let's make sure the right side isn't empty,
                        // which is the case when the cell's edge is a border edge.
                        if (currentEdge.SiteRight != null)
                        {
                            currentCell.Neighbours.Add(currentEdge, this.Polygons.Cells[currentEdge.SiteRight.ID]);
                        }
                    }
                    else
                    {
                        // The cell is right, let's grab left.
                        currentCell.Neighbours.Add(currentEdge, this.Polygons.Cells[currentEdge.SiteLeft.ID]);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DetermineCellNeighbours", ex);
            }
        }

        /// <summary>
        /// Calculates perlin noise and uses it to determine the
        /// elevation of each point in a polygon.
        /// </summary>
        private void DetermineCellNoise(Cell currentCell)
        {
            try
            {
                // While we determine the noise for each of the points that make up the polygon, we also want to use those
                // points to determine the overall noise for the cell. So let's add all the noise together.
                double dblTotalNoise = 0;

                foreach (Point currentPoint in currentCell.Points)
                {
                    // Calculate noise, but because points are used across different cells, we only have to do it once.
                    if (currentPoint.Noise == 0)
                    {
                        double dblNoise = this.Perlin.Noise(this.NoiseOctaves * currentPoint.x / this.Width, this.NoiseOctaves * currentPoint.y / this.Height, 0);
                        currentPoint.Noise = (dblNoise + 1) / 2;
                    }
                    dblTotalNoise += currentPoint.Noise;
                }

                // Now that we've calculated the noise for each of the points, use that to determine the overall noise of the
                // cell by setting the noise for the site of the cell to the average of the noise of the individual points.
                currentCell.Site.Noise = (dblTotalNoise / currentCell.Points.Count);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DetermineCellNoise", ex);
            }
        }

        /// <summary>
        /// This tries to create a shape for the island based on radial, overlapping sine waves.
        /// </summary>
        private void IslandTypeRadial()
        {
            try
            {
                // Log.
                this.Write("Start with IslandType");

                // The island factor is between 1 and 2 and determines the amount of smaller, outlying islands there will be.
                // The lower the number, the fewer will occur. The higher the number, the more small islands you'll see.
                double dblIslandFactor = 1.07;

                // This will determine how many percent of the corners of a cell being water it takes for the cell to become
                // water. The higher the number, the more corners have to qualify in order for the cell to be water.
                double dblLakeThreshold = 0.2;

                // This number, between 0 and 1, will determine just how large the island will become. The higher the number
                // the larger the island becomes.
                double dblIslandSize = 1.0;

                // Initialise some random elements, like the amount of bumps the island will have, etc.
                int intBumps = this.Random.Next(1, 6);
                double dblStartAngle = this.Random.NextDoubleRange(0, 2 * Math.PI);
                double dblDipAngle = this.Random.NextDoubleRange(0, 2 * Math.PI);
                double dblDipWidth = this.Random.NextDoubleRange(0.2, 0.7);

                // Now go through all the cells.
                foreach (KeyValuePair<int, Cell> kvpCell in this.Polygons.Cells)
                {
                    // Reset the counter for the amount of water points the cell has gathered.
                    int intWaterPoint = 0;

                    // Now determine the elevation for each of the points that make up the cell.
                    foreach (Point currentPoint in kvpCell.Value.Points)
                    {
                        var currentPointCalc = new Point()
                        {
                            x = 2 * (currentPoint.x / this.Width - 0.5),
                            y = 2 * (currentPoint.y / this.Height - 0.5)
                        };

                        double dblAngle = Math.Atan2(currentPointCalc.y, currentPointCalc.x);
                        double dblLength = (1 - dblIslandSize) * Math.Max(Math.Abs(currentPointCalc.x), Math.Abs(currentPointCalc.y)) + currentPointCalc.Length;

                        double r1 = 0.5 + 0.40 * Math.Sin(dblStartAngle + intBumps * dblAngle + Math.Cos((intBumps + 3) * dblAngle));
                        double r2 = 0.7 - 0.20 * Math.Sin(dblStartAngle + intBumps * dblAngle - Math.Sin((intBumps + 2) * dblAngle));

                        if (Math.Abs(dblAngle - dblDipAngle) < dblDipWidth || Math.Abs(dblAngle - dblDipAngle + 2 * Math.PI) < dblDipWidth || Math.Abs(dblAngle - dblDipAngle - 2 * Math.PI) < dblDipWidth)
                        {
                            r1 = r2 = 0.2;
                        }

                        if (dblLength < r1 || (dblLength > r1 * dblIslandFactor && dblLength < r2))
                        {
                            // Land
                        }
                        else
                        {
                            // Water
                            intWaterPoint++;
                        }
                    }

                    // Now that we've determined wether the points of the cell are water or land, see if the cell itself becomes water.
                    if (kvpCell.Value.Biome != Biomes.Ocean && intWaterPoint >= kvpCell.Value.Points.Count * dblLakeThreshold)
                    {
                        kvpCell.Value.Biome = Biomes.Water;
                    }
                    else
                    {
                        kvpCell.Value.Biome = Biomes.Land;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in IslandType", ex);
            }
            finally
            {
                this.Write("End with IslandType");
            }
        }

        /// <summary>
        /// This tries to create a shape for the island based on perlin noise.
        /// </summary>
        private void IslandTypePerlin()
        {
            try
            {
                // Log.
                this.Write("Start with IslandTypePerlin");

                // Let's prepare some radial information. This basically determines how far away from the middle the cell is.
                double dblOceanRatio = 0.5;
                double dblMinimumLandRatio = 0.1;
                double dblMaximumLandRatio = 0.5;

                // Determine ocean ratio.
                dblOceanRatio = (dblMaximumLandRatio - dblMinimumLandRatio) * dblOceanRatio + dblMinimumLandRatio;

                // Now go through all the cells.
                foreach (KeyValuePair<int, Cell> kvpCell in this.Polygons.Cells)
                {
                    // Grab the cell
                    Cell currentCell = kvpCell.Value;

                    // Noise has already been calculated for each of the points in a cell, and thus for the cell itself.
                    // Now we need to compare that value to a radial value of the cell.
                    double dx = (currentCell.Site.x - (this.Width / 2)) / (this.Width / 2);
                    double dy = (currentCell.Site.y - (this.Height / 2)) / (this.Height / 2);
                    double dblRadial = dblOceanRatio + dblOceanRatio * Math.Sqrt(dx * dx + dy * dy) * Math.Sqrt(dx * dx + dy * dy);

                    if (currentCell.Site.Noise < dblRadial)
                    {
                        currentCell.Biome = Biomes.Water;
                    }
                    else
                    {
                        currentCell.Biome = Biomes.Land;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in IslandTypePerlin", ex);
            }
            finally
            {
                this.Write("End with IslandTypePerlin");
            }
        }

        /// <summary>
        /// Determine which cells are oceans.
        /// </summary>
        private void DetermineOcean()
        {
            try
            {
                // Log.
                this.Write("Start with DetermineOcean");

                // Place all the cells that are considered outer edges in the queue.
                var lstQueue = new Queue<Cell>(this.Polygons.Cells.Where(c => c.Value.IsOuterEdge).Select(c => c.Value));

                // Now we'll go through the queue and look at the adjascent cells. If they're water, they'll also be marked as oceans.
                // Then they'll be added to the stack for inspection. This way, by the time we're done, any water cells that are left
                // are considered lakes, not oceans.
                while (lstQueue.Count > 0)
                {
                    // Grab the cell.
                    Cell currentCell = lstQueue.Dequeue();

                    // First let's set the cell as an ocean cell.
                    currentCell.Biome = Biomes.Ocean;

                    // Inspect its neighbours and grab all the water cells.
                    var lstNeighbours = currentCell.Neighbours.Where(c => c.Value.Biome == Biomes.Water).Select(c => c.Value).ToList<Cell>();

                    // Add each of the cells to the queue.
                    lstNeighbours.ForEach(c => lstQueue.Enqueue(c));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DetermineOcean", ex);
            }
            finally
            {
                this.Write("End with DetermineOcean");
            }
        }

        /// <summary>
        /// Determines which edges border both land and water and marks them as coastlines.
        /// </summary>
        private void DetermineCoast()
        {
            try
            {
                // Log.
                this.Write("Start with DetermineCoast");

                // Let's find all water cells. Unfortunately, we can't do this in the same 
                // function as DetermineOcean(), since that floodfills all the water as
                // oceans starting at the edges.
                var lstQueue = new Stack<Cell>(this.Polygons.Cells.Where(c => c.Value.Biome == Biomes.Water || c.Value.Biome == Biomes.Ocean).Select(c => c.Value));

                while (lstQueue.Count > 0)
                {
                    // Grab the cell.
                    Cell currentCell = lstQueue.Pop();

                    // Inspect its neighbours.
                    foreach (KeyValuePair<Edge, Cell> kvpNeighbour in currentCell.Neighbours)
                    {
                        // Grab the neighbour cell.
                        Cell currentNeighbour = kvpNeighbour.Value;

                        // If the neighbour is not land we can continue, otherwise we'll mark the edge as a coastline.
                        if (currentNeighbour.Biome == Biomes.Water || currentNeighbour.Biome == Biomes.Ocean) { continue; }

                        // Grab the connecting edge.
                        Edge currentEdge = kvpNeighbour.Key;

                        // Mark this as a coastline.
                        currentEdge.Type = EdgeType.Coast;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DetermineCoast", ex);
            }
            finally
            {
                this.Write("End with DetermineCoast");
            }
        }

        /// <summary>
        /// Determines elevation (or depth) for each cell.
        /// </summary>
        private void DetermineCellElevation()
        {
            try
            {
                // Log.
                this.Write("Start with DetermineCellElevation");

                // Initialise variables.
                double dblWater = 0.001;
                double dblLand = 1;

                // Grab the border cells.
                var lstQueue = new Queue<Cell>(this.Polygons.Cells.Where(c => c.Value.IsOuterEdge).Select(c => c.Value));

                // Give all the border cells an elevation index of 0.001.
                foreach (Cell currentCell in lstQueue)
                {
                    currentCell.ElevationIndex = dblWater;
                }

                // Cycle through the cells and process each of the neighbouring cells that have an elevation index of -1. Give them an elevation index
                // of 0.01 if it's a water cell and 1 if it's a land cell, added to the elevation index of the one they're adjescent to. Finally, add 
                // them to the queue, so that we can process their neighbours as well.
                while (lstQueue.Count > 0)
                {
                    // Grab and remove the first cell in the queue.
                    Cell currentCell = lstQueue.Dequeue();

                    foreach (KeyValuePair<Edge, Cell> kvpNeighbour in currentCell.Neighbours)
                    {
                        // Grab the neighbouring cell.
                        Cell currentNeighbour = kvpNeighbour.Value;

                        // Check to see if it has an elevation index. If so, we can continue with the next one.
                        if (currentNeighbour.ElevationIndex != -1) { continue; }

                        // It doesn't have an elevation index yet. Give it one by adding the previous cell
                        // to either 0.01 if it's water or 1 if it's land.
                        if (currentNeighbour.Biome == Biomes.Ocean || currentNeighbour.Biome == Biomes.Water)
                        {
                            currentNeighbour.ElevationIndex = currentCell.ElevationIndex + dblWater;
                        }
                        else
                        {
                            currentNeighbour.ElevationIndex = currentCell.ElevationIndex + dblLand;
                        }

                        // Lastly, we'll add the neighbour to the queue so we can process its neighbours too.
                        lstQueue.Enqueue(currentNeighbour);
                    }
                }

                // Make sure to remember the highest elevation index. We will need this to normalise the elevation index
                // once all elevation indices have been determined.
                double dblMaximumElevationIndex = this.Polygons.Cells.Max(c => c.Value.ElevationIndex);
                double dblMinimumElevationIndex = this.Polygons.Cells.Min(c => c.Value.ElevationIndex);

                // Now let's determine elevation or depth of each cell.
                foreach (KeyValuePair<int, Cell> kvpCell in this.Polygons.Cells)
                {
                    // Produce an elevation index between 0 and 1.
                    kvpCell.Value.ElevationIndex = kvpCell.Value.ElevationIndex / dblMaximumElevationIndex;

                    kvpCell.Value.DetermineElevation(this.Peak);
                }

                // Let's grab the maximum and minimum elevation.
                double dblMaximumLandElevation = this.Polygons.Cells.Where(c => c.Value.Biome != Biomes.Ocean && c.Value.Biome != Biomes.Water).Select(c => c.Value.Site.z).DefaultIfEmpty().Max();
                double dblMinimumLandElevation = this.Polygons.Cells.Where(c => c.Value.Biome != Biomes.Ocean && c.Value.Biome != Biomes.Water).Select(c => c.Value.Site.z).DefaultIfEmpty().Min();
                double dblMaximumWaterDepth = this.Polygons.Cells.Where(c => c.Value.Biome == Biomes.Ocean || c.Value.Biome == Biomes.Water).Min(c => c.Value.Site.z);
                double dblMinimumWaterDepth = this.Polygons.Cells.Where(c => c.Value.Biome == Biomes.Ocean || c.Value.Biome == Biomes.Water).Max(c => c.Value.Site.z);

                // Now that we have the highs and lows, we'll determine elevation and depth zones.
                foreach (KeyValuePair<int, Cell> kvpCell in this.Polygons.Cells)
                {
                    // Grab the cell.
                    Cell currentCell = kvpCell.Value;

                    // Determine the elevation zone.
                    if (currentCell.Biome == Biomes.Ocean || currentCell.Biome == Biomes.Water)
                    {
                        // See what the relative depth is of the site.
                        double dblRelativeDepth = ((currentCell.Site.z - dblMinimumWaterDepth) / (dblMaximumWaterDepth - dblMinimumWaterDepth)) * 100;

                        if (dblRelativeDepth <= 30)
                        {
                            currentCell.ElevationZone = ElevationZones.Shallow;
                        }
                        else if (dblRelativeDepth <= 80)
                        {
                            currentCell.ElevationZone = ElevationZones.Deep;
                        }
                        else
                        {
                            currentCell.ElevationZone = ElevationZones.Trench;
                        }
                    }
                    else
                    {
                        // See what the relative elevation is of the site.
                        double dblRelativeElevation = ((currentCell.Site.z - dblMinimumLandElevation) / (dblMaximumLandElevation - dblMinimumLandElevation)) * 100;

                        // Based on this we'll determine the elevation zone.
                        if (dblRelativeElevation <= 30)
                        {
                            currentCell.ElevationZone = ElevationZones.Low;
                        }
                        else if (dblRelativeElevation <= 65)
                        {
                            currentCell.ElevationZone = ElevationZones.LowerMiddle;
                        }
                        else if (dblRelativeElevation <= 85)
                        {
                            currentCell.ElevationZone = ElevationZones.UpperMiddle;
                        }
                        else
                        {
                            currentCell.ElevationZone = ElevationZones.High;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DetermineCellElevation", ex);
            }
            finally
            {
                this.Write("End with DetermineCellElevation");
            }
        }

        /// <summary>
        /// Creates a random amount of rivers.
        /// </summary>
        private void DetermineRivers()
        {
            try
            {
                // Log.
                this.Write("Start with DetermineRivers");

                // Pick a random amount of rivers.
                for (int intCount = 0; intCount < (this.Width / 2); intCount++)
                {
                    // Get a random site.
                    int intSiteID = this.Polygons.SiteList[this.Random.Next(0, this.Polygons.Cells.Count - 1)].ID;

                    // Check to see if the corresponding cell is present. If not, continue with the next site.
                    if (!this.Polygons.Cells.ContainsKey(intSiteID)) { continue; }

                    // Grab the cell corresponding to the random site.
                    Cell currentCell = this.Polygons.Cells[intSiteID];

                    // Check whether the elevation zone is correct. If not, continue with the next site.
                    if (currentCell.ElevationZone != ElevationZones.UpperMiddle && currentCell.ElevationZone != ElevationZones.High) { continue; }

                    // Let's grab the highest point in the cell.
                    Edge currentEdge = null;

                    foreach (HalfEdge currentHalfEdge in currentCell.HalfEdges)
                    {
                        // If this is the first edge, just grab that one and continue.
                        if (currentEdge == null)
                        {
                            currentEdge = currentHalfEdge.Edge;
                            continue;
                        }

                        // Compare the remembered edge with the current one and see which one has the highest point.
                        if (Math.Max(currentHalfEdge.Edge.VertexA.z, currentHalfEdge.Edge.VertexB.z) > Math.Max(currentEdge.VertexA.z, currentEdge.VertexB.z))
                        {
                            currentEdge = currentHalfEdge.Edge;
                        }
                    }

                    // Now we have the edge with the highest point for the cell. Let's mark it as a river.
                    currentEdge.Type = EdgeType.River;
                    currentEdge.River++;

                    // Let's follow the river down towards a coastline.
                    while (true)
                    {
                        // First we need to find out which of the two vertices on the edge is the lowest, that's the one we'll start with.
                        Point currentPoint = (currentEdge.VertexA.z < currentEdge.VertexB.z) ? currentEdge.VertexA : currentEdge.VertexB;

                        // Now we have all connecting edges. Let's find the one with the steepest decline.
                        Edge currentNextEdge = null;
                        Point currentNextPoint = null;
                        foreach (KeyValuePair<Edge, Point> kvpEdge in currentPoint.Neighbours)
                        {
                            // Ignore the current edge from the collection of neighbours for this point.
                            if (currentEdge == kvpEdge.Key) { continue; }

                            // If we don't have an edge, let's grab the first one and continue.
                            if (currentNextEdge == null)
                            {
                                currentNextEdge = kvpEdge.Key;
                                currentNextPoint = kvpEdge.Value;
                                continue;
                            }

                            if (currentNextPoint.z > kvpEdge.Value.z)
                            {
                                currentNextEdge = kvpEdge.Key;
                                currentNextPoint = kvpEdge.Value;
                            }
                        }

                        // Check to see if we managed to find an edge. (We should be able to, since no point only has one edge.
                        if (currentNextEdge == null)
                        {
                            break;
                        }

                        // We have found the lowest point connecting to the previous point. Let's see if it's lower than the previous point.
                        if (currentPoint.z < currentNextPoint.z)
                        {
                            // The next edge is not going down, the river ends here.
                            break;
                        }

                        // Check whether the edge is a coast line. If so, we're done.
                        if (currentNextEdge.Type == EdgeType.Coast)
                        {
                            break;
                        }

                        // We've found our next edge in the river.
                        currentEdge = currentNextEdge;
                        currentEdge.Type = EdgeType.River;
                        currentEdge.River++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DetermineRivers", ex);
            }
            finally
            {
                this.Write("End with DetermineRivers");
            }
        }

        /// <summary>
        /// Draw the map.
        /// </summary>
        private void DrawMap()
        {
            try
            {
                // Log.
                this.Write("Start with DrawMap");

                // Initialise variables.
                var currentMap = new StringBuilder();

                // Create the vector header.
                currentMap.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                currentMap.AppendLine("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">");
                currentMap.AppendLine(string.Format("<svg height=\"{0}px\" width=\"{1}px\" viewBox=\"0 0 {2} {3}\" version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\">", this.Height, this.Width, this.Width, this.Height));

                foreach (KeyValuePair<int, Cell> kvpCell in this.Polygons.Cells)
                {
                    // Grab the cell.
                    Cell currentCell = kvpCell.Value;

                    // Initialise a few variables.
                    string strCellColour = "transparent";
                    string strBorderColour = "#000000";

                    if (this.MapType == MapType.Elevation)
                    {
                        int intLevel = (int)Math.Round(currentCell.Site.Noise * 255, 0);

                        int intR = 0;
                        int intG = 0;
                        int intB = 0;

                        if (currentCell.Biome == Biomes.Water || currentCell.Biome == Biomes.Ocean)
                        {
                            intR = 0;
                            intG = (255 - intLevel);
                            //intG = intLevel;
                            intB = 255;
                        }
                        else
                        {
                            intR = intLevel;
                            intG = 255;
                            intB = intLevel;
                        }
                        //int intR = (currentCell.Biome == Biomes.Water || currentCell.Biome == Biomes.Ocean) ? 0 : intLevel;
                        //int intG = (currentCell.Biome != Biomes.Water && currentCell.Biome != Biomes.Ocean) ? 255 : intLevel;
                        //int intB = (currentCell.Biome == Biomes.Water || currentCell.Biome == Biomes.Ocean) ? 255 : intLevel;
                        strCellColour = System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(intR, intG, intB));
                    }
                    else if (this.MapType == MapType.ElevationZones)
                    {
                        switch (currentCell.ElevationZone)
                        {
                            case ElevationZones.High:
                                strCellColour = "brown";
                                break;
                            case ElevationZones.UpperMiddle:
                                strCellColour = "red";
                                break;
                            case ElevationZones.LowerMiddle:
                                strCellColour = "orange";
                                break;
                            case ElevationZones.Low:
                                strCellColour = "yellow";
                                break;
                            case ElevationZones.Shallow:
                                strCellColour = "blue";
                                break;
                            case ElevationZones.Deep:
                                strCellColour = "darkblue";
                                break;
                            case ElevationZones.Trench:
                                strCellColour = "midnightblue";
                                break;
                            default:
                                throw new NotImplementedException("This elevation zone doesn't exist.");
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("The only map type that's currently been implemented is the elevation type.");
                    }

                    // If we can't see the borders, we're just simply going to change the border colour to the colour of the cell.
                    if (!this.ShowBorders)
                    {
                        strBorderColour = strCellColour;
                    }

                    // First we need to make a new group.
                    currentMap.AppendLine("  <g>");

                    // Create polygon.
                    currentMap.AppendLine(string.Format("    <polygon id=\"poly{0}\" points=\"{1}\" style=\"stroke: {2}; stroke-width: 0.5; fill: {3};\" />", currentCell.Site.ID, currentCell.PolygonPoints, strBorderColour, strCellColour));

                    // Create the main site for the polygon, but only if we're showing borders.
                    if (this.ShowBorders)
                    {
                        currentMap.AppendLine(string.Format("    <circle cx=\"{0}\" cy=\"{1}\" r=\"1\" style=\"stroke-width: 0.5;\" />", currentCell.Site.x, currentCell.Site.y));
                    }

                    // Create the polygon borders, but only if we're showing borders.
                    if (this.ShowBorders)
                    {
                        var lstCoasts = currentCell.HalfEdges.Where(h => h.Edge.Type == EdgeType.Coast).Select(h => h.Edge).ToList<Edge>();

                        foreach (Edge currentEdge in lstCoasts)
                        {
                            currentMap.AppendLine(string.Format("    <line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" style=\"stroke: black; stroke-width: 2;\" />", currentEdge.VertexA.x.ToString().Replace(",", "."), currentEdge.VertexA.y.ToString().Replace(",", "."), currentEdge.VertexB.x.ToString().Replace(",", "."), currentEdge.VertexB.y.ToString().Replace(",", ".")));
                        }

                        lstCoasts.Clear();
                        lstCoasts = null;
                    }

                    // Create the rivers.
                    var lstRivers = currentCell.HalfEdges.Where(h => h.Edge.Type == EdgeType.River).Select(h => h.Edge).ToList<Edge>();

                    foreach (Edge currentEdge in lstRivers)
                    {
                        currentMap.AppendLine(string.Format("    <line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{3}\" style=\"stroke: blue; stroke-width: 2;\" />", currentEdge.VertexA.x.ToString().Replace(",", "."), currentEdge.VertexA.y.ToString().Replace(",", "."), currentEdge.VertexB.x.ToString().Replace(",", "."), currentEdge.VertexB.y.ToString().Replace(",", ".")));
                    }

                    lstRivers.Clear();
                    lstRivers = null;

                    // Create a tooltip for the cell.
                    currentMap.AppendLine(string.Format("    <text x=\"{0}\" y=\"{1}\" visibility=\"hidden\">", currentCell.Site.x + 3, currentCell.Site.y + 3));
                    currentMap.AppendLine(string.Format("      ID: {0}~br~", currentCell.Site.ID));
                    currentMap.AppendLine(string.Format("      Coordinates: {0}x{1}~br~", currentCell.Site.x, currentCell.Site.y));
                    currentMap.AppendLine(string.Format("      Elevation: {0}~br~", currentCell.Site.z));
                    currentMap.AppendLine(string.Format("      Elevation Zone: {0}~br~", currentCell.ElevationZone.ToString()));
                    currentMap.AppendLine(string.Format("      Biome: {0}~br~", currentCell.Biome.ToString()));
                    currentMap.AppendLine(string.Format("      Noise: {0}~br~", currentCell.Site.Noise));
                    currentMap.AppendLine(string.Format("      Elevation Index: {0}~br~", currentCell.ElevationIndex));
                    currentMap.AppendLine("    </text>");
                    currentMap.AppendLine("  </g>");
                }

                // Close the header.
                currentMap.AppendLine("</svg>");

                // Now let's put the HTML into the pass-through property.
                this.VectorMap = currentMap.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DrawMap", ex);
            }
            finally
            {
                this.Write("End with DrawMap");
            }
        }

        /// <summary>
        /// Write debug information.
        /// </summary>
        private void Write(string strMessage)
        {
            try
            {
                long lngElapsed = this.Stopwatch.ElapsedMilliseconds;

                //using (var currentWriter = new System.IO.StreamWriter(this.Log, true))
                //{
                //    currentWriter.WriteLine(string.Format("{0}\t{1}ms\t{2}", DateTime.Now, lngElapsed, strMessage));
                //}

                this.Debug.AppendLine(string.Format("{0}ms: {1}<br>", lngElapsed, strMessage));
            }
            catch
            {
            }
        }
    }
}