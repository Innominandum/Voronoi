using System;
using System.Collections.Generic;

namespace Voronoi.Objects
{
    public class Transitions
    {
        #region Properties

        public LinkedList<int> InternalIndex = new LinkedList<int>();
        public Dictionary<int, Beach> InternalTransitions = new Dictionary<int, Beach>();
        private int RunningIndex = -1;
        public int Count
        {
            get
            {
                return this.RunningIndex + 1;
            }
        }

        #endregion

        /// <summary>
        /// Add the beach to the front of the list.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-28</date>
        public void AddFirst(Beach objBeach)
        {
            this.RunningIndex++;
            this.InternalTransitions.Add(this.RunningIndex, objBeach);
            this.InternalIndex.AddFirst(this.RunningIndex);
        }

        /// <summary>
        /// Add the beach to the end of the list.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-28</date>
        public void AddLast(Beach objBeach)
        {
            this.RunningIndex++;
            this.InternalTransitions.Add(this.RunningIndex, objBeach);
            this.InternalIndex.AddLast(this.RunningIndex);
        }

        /// <summary>
        /// Get a particular item from the list.
        /// </summary>
        /// <created_by>Dennis Steinmeijer</created_by>
        /// <date>2013-07-28</date>
        public Beach Item(int intExternalIndex)
        {
            // Initialise variables.
            int intIndexFound = -1;
            int intInternalIndex = 0;

            foreach (int intIndex in this.InternalIndex)
            {
                // Check to see if we've reach the right index.
                if (intExternalIndex == intInternalIndex)
                {
                    intIndexFound = intIndex;
                    break;
                }

                // Raise the internal index.
                intInternalIndex++;
            }

            if (intIndexFound == -1) { throw new Exception("Item not found!"); }

            if (!this.InternalTransitions.ContainsKey(intIndexFound)) { throw new Exception("Item not found in dictionary!"); }

            return this.InternalTransitions[intIndexFound];
        }
    }
}
