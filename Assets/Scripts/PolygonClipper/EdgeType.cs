namespace Martinez
{
    /// <summary>
    /// Defines the type of edge contribution in the clipping algorithm.
    /// </summary>
    public enum EdgeType
    {
        /// <summary>
        /// Edge contributes normally to the result.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Edge does not contribute to the result.
        /// </summary>
        NonContributing = 1,

        /// <summary>
        /// Edge has the same transition as another edge.
        /// </summary>
        SameTransition = 2,

        /// <summary>
        /// Edge has a different transition than another edge.
        /// </summary>
        DifferentTransition = 3
    }
}


