using UnityEngine;
using System.Collections.Generic;

namespace Live2D.Cubism.Framework.LookAt
{
    /// <summary>
    /// Advanced look target interface that supports multiple look targets.
    /// </summary>
    public interface IAdvancedLookTarget
    {
        /// <summary>
        /// Gets the position for a specific look index.
        /// </summary>
        /// <param name="lookIndex">The index of the look target to get the position for.</param>
        Vector3 GetLookTargetPosition(int lookIndex);

        /// <summary>
        /// Gets all available look target indices.
        /// </summary>
        IEnumerable<int> GetAvailableLookIndices();

        /// <summary>
        /// Whether the target is active.
        /// </summary>
        bool IsActive();
    }
}