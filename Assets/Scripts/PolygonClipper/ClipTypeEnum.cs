namespace Martinez
{
    /// <summary>
    /// Defines the type of clipping operation to perform on polygons.
    /// </summary>
    public enum ClipType
    {
        /// <summary>
        /// Combines two polygons, resulting in areas covered by either polygon.
        /// </summary>
        Union,

        /// <summary>
        /// Keeps only areas covered by both polygons.
        /// </summary>
        Intersection,

        /// <summary>
        /// Keeps areas of the subject polygon not covered by the clipping polygon.
        /// </summary>
        Difference,

        /// <summary>
        /// Keeps areas covered by exactly one polygon (either subject or clipping, but not both).
        /// </summary>
        Xor
    };
}
