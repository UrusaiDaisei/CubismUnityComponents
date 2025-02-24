/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core.Unmanaged;
using Live2D.Cubism.Framework;
using UnityEngine;


namespace Live2D.Cubism.Core
{
    /// <summary>
    /// Single <see cref="CubismModel"/> drawable.
    /// </summary>
    [CubismDontMoveOnReimport]
    public sealed class CubismDrawable : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// <see cref="UnmanagedIndex"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _unmanagedIndex = -1;

        /// <summary>
        /// <see cref="MultiplyColor"/> backing field.
        /// </summary>
        private Color _multiplyColor;

        /// <summary>
        /// <see cref="ScreenColor"/> backing field.
        /// </summary>
        public Color _screenColor;

        #endregion

        #region Runtime

        /// <summary>
        /// Unmanaged drawables from unmanaged model.
        /// </summary>
        private CubismUnmanagedDrawables UnmanagedDrawables { get; set; }

        private Vector3[] _cachedVertexPositions;
        private Vector2[] _cachedVertexUvs;
        private int[] _cachedIndices;
        private CubismDrawable[] _cachedMasks;
        private bool _isVertexPositionsDirty = true;

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates drawables for a <see cref="CubismModel"/>.
        /// </summary>
        /// <param name="unmanagedModel">Handle to unmanaged model.</param>
        /// <returns>Drawables root.</returns>
        internal static GameObject CreateDrawables(CubismUnmanagedModel unmanagedModel)
        {
            var root = new GameObject("Drawables");

            // Create drawables.
            var unmanagedDrawables = unmanagedModel.Drawables;
            var buffer = new CubismDrawable[unmanagedDrawables.Count];

            for (var i = 0; i < buffer.Length; ++i)
            {
                var proxy = new GameObject();

                buffer[i] = proxy.AddComponent<CubismDrawable>();

                buffer[i].transform.SetParent(root.transform);
                buffer[i].Reset(unmanagedModel, i);
            }

            return root;
        }

        #endregion

        #region Public Methods and Structs

        /// <summary>
        /// Parent Part Position in unmanaged arrays.
        /// </summary>
        public int UnmanagedParentIndex => UnmanagedDrawables.ParentPartIndices[UnmanagedIndex];

        /// <summary>
        /// Copy of Id.
        /// </summary>
        public string Id => UnmanagedDrawables.Ids[UnmanagedIndex];

        /// <summary>
        /// Texture UnmanagedIndex.
        /// </summary>
        public int TextureIndex => UnmanagedDrawables.TextureIndices[UnmanagedIndex];

        /// <summary>
        /// Copy of MultiplyColor.
        /// </summary>
        public Color MultiplyColor
        {
            get
            {
                var index = UnmanagedIndex * 4;
                _multiplyColor.r = UnmanagedDrawables.MultiplyColors[index];
                _multiplyColor.g = UnmanagedDrawables.MultiplyColors[index + 1];
                _multiplyColor.b = UnmanagedDrawables.MultiplyColors[index + 2];
                _multiplyColor.a = UnmanagedDrawables.MultiplyColors[index + 3];
                return _multiplyColor;
            }
        }

        /// <summary>
        /// Copy of ScreenColor.
        /// </summary>
        public Color ScreenColor
        {
            get
            {
                var index = UnmanagedIndex * 4;
                _screenColor.r = UnmanagedDrawables.ScreenColors[index];
                _screenColor.g = UnmanagedDrawables.ScreenColors[index + 1];
                _screenColor.b = UnmanagedDrawables.ScreenColors[index + 2];
                _screenColor.a = UnmanagedDrawables.ScreenColors[index + 3];
                return _screenColor;
            }
        }

        public int ParentPartIndex
        {
            get
            {
                return UnmanagedDrawables.ParentPartIndices[UnmanagedIndex];
            }
        }

        #endregion

        #region Internal Methods and Structs

        /// <summary>
        /// Position in unmanaged arrays.
        /// </summary>
        internal int UnmanagedIndex
        {
            get => _unmanagedIndex;
            private set => _unmanagedIndex = value;
        }

        internal void SetVertexPositionsDirty()
        {
            _isVertexPositionsDirty = true;
        }

        /// <summary>
        /// Revives instance.
        /// </summary>
        /// <param name="unmanagedModel">Handle to unmanaged model.</param>
        internal void Revive(CubismUnmanagedModel unmanagedModel)
        {
            UnmanagedDrawables = unmanagedModel.Drawables;
            _isVertexPositionsDirty = true;
        }

        #endregion

        #region Auxiliary Code

        private void InitializeVertexPositions()
        {
            if (_cachedVertexPositions != null) return;

            var vertexCount = UnmanagedDrawables.VertexCounts[UnmanagedIndex];
            _cachedVertexPositions = new Vector3[vertexCount];
            _isVertexPositionsDirty = true;
        }

        private void InitializeUvs()
        {
            if (_cachedVertexUvs != null) return;

            var vertexCount = UnmanagedDrawables.VertexCounts[UnmanagedIndex];
            var uvs = UnmanagedDrawables.VertexUvs[UnmanagedIndex];

            _cachedVertexUvs = new Vector2[vertexCount];
            for (var i = 0; i < vertexCount; i++)
            {
                var baseIndex = i * 2;
                _cachedVertexUvs[i] = new Vector2(
                    uvs[baseIndex],
                    uvs[baseIndex + 1]
                );
            }
        }

        private void InitializeIndices()
        {
            if (_cachedIndices != null) return;

            var indexCount = UnmanagedDrawables.IndexCounts[UnmanagedIndex];
            var indices = UnmanagedDrawables.Indices[UnmanagedIndex];

            _cachedIndices = new int[indexCount];
            for (var i = 0; i < indexCount; i++)
            {
                _cachedIndices[i] = indices[i];
            }
        }

        private void InitializeMasks()
        {
            if (_cachedMasks != null) return;

            var drawables = this.FindCubismModel(true).Drawables;
            var counts = UnmanagedDrawables.MaskCounts;
            var indices = UnmanagedDrawables.Masks;

            _cachedMasks = new CubismDrawable[counts[UnmanagedIndex]];

            for (var i = 0; i < _cachedMasks.Length; ++i)
            {
                for (var j = 0; j < drawables.Length; ++j)
                {
                    if (drawables[j].UnmanagedIndex != indices[UnmanagedIndex][i])
                    {
                        continue;
                    }

                    _cachedMasks[i] = drawables[j];
                    break;
                }
            }
        }

        private void UpdateVertexPositions()
        {
            if (!_isVertexPositionsDirty) return;

            var positions = UnmanagedDrawables.VertexPositions[UnmanagedIndex];

            for (var i = 0; i < _cachedVertexPositions.Length; i++)
            {
                var baseIndex = i * 2;
                _cachedVertexPositions[i] = new Vector3(
                    positions[baseIndex],
                    positions[baseIndex + 1]
                );
            }

            _isVertexPositionsDirty = false;
        }

        /// <summary>
        /// Restores instance to initial state.
        /// </summary>
        /// <param name="unmanagedModel">Handle to unmanaged model.</param>
        /// <param name="unmanagedIndex">Position in unmanaged arrays.</param>
        private void Reset(CubismUnmanagedModel unmanagedModel, int unmanagedIndex)
        {
            Revive(unmanagedModel);
            UnmanagedIndex = unmanagedIndex;
            name = Id;

            // Clear cache to force reinitialization
            _cachedVertexPositions = null;
            _cachedVertexUvs = null;
            _cachedIndices = null;
            _cachedMasks = null;
        }

        #endregion

        /// <summary>
        /// Copy of vertex positions.
        /// </summary>
        public Vector3[] VertexPositions
        {
            get
            {
                InitializeVertexPositions();
                UpdateVertexPositions();
                return _cachedVertexPositions;
            }
        }

        /// <summary>
        /// Copy of vertex texture coordinates.
        /// </summary>
        public Vector2[] VertexUvs
        {
            get
            {
                InitializeUvs();
                return _cachedVertexUvs;
            }
        }

        /// <summary>
        /// Copy of triangle indices.
        /// </summary>
        public int[] Indices
        {
            get
            {
                InitializeIndices();
                return _cachedIndices;
            }
        }

        /// <summary>
        /// Copy of the masks.
        /// </summary>
        public CubismDrawable[] Masks
        {
            get
            {
                InitializeMasks();
                return _cachedMasks;
            }
        }

        /// <summary>
        /// True if double-sided.
        /// </summary>
        public bool IsDoubleSided
        {
            get
            {
                // Get address.
                var flags = UnmanagedDrawables.ConstantFlags;

                // Pull data.
                return flags[UnmanagedIndex].HasIsDoubleSidedFlag();
            }
        }

        /// <summary>
        /// True if masking is requested.
        /// </summary>
        public bool IsMasked
        {
            get
            {
                // Get address.
                var counts = UnmanagedDrawables.MaskCounts;

                // Pull data.
                return counts[UnmanagedIndex] > 0;
            }
        }

        /// <summary>
        /// True if inverted mask.
        /// </summary>
        public bool IsInverted
        {
            get
            {
                // Get address.
                var flags = UnmanagedDrawables.ConstantFlags;

                // Pull data.
                return flags[UnmanagedIndex].HasIsInvertedMaskFlag();
            }
        }

        /// <summary>
        /// True if additive blending is requested.
        /// </summary>
        public bool BlendAdditive
        {
            get
            {
                // Get address.
                var flags = UnmanagedDrawables.ConstantFlags;

                // Pull data.
                return flags[UnmanagedIndex].HasBlendAdditiveFlag();
            }
        }

        /// <summary>
        /// True if multiply blending is setd.
        /// </summary>
        public bool MultiplyBlend
        {
            get
            {
                // Get address.
                var flags = UnmanagedDrawables.ConstantFlags;

                // Pull data.
                return flags[UnmanagedIndex].HasBlendMultiplicativeFlag();
            }
        }
    }
}
