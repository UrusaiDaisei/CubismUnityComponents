using UnityEngine;

namespace Live2D.Cubism.Framework
{
    /// <summary>
    /// Component for managing parameter groups in a Live2D model.
    /// </summary>
    public sealed class CubismParameterGroups : MonoBehaviour
    {
        [System.Serializable]
        public struct ParameterGroup
        {
            public string Id;
            public string Name;
            public CubismDisplayInfoParameterName[] Parameters;
        }

        public ParameterGroup[] Groups;
    }
}