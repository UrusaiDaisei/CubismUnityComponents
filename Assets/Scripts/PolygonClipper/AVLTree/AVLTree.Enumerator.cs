using System;
using System.Collections;
using System.Collections.Generic;

namespace Martinez
{
    /*
    partial class AVLTree<T>
    {
        /// <summary>
        /// Enumerator class for traversing the AVL tree in-order.
        /// </summary>
        internal sealed class Enumerator : IEnumerator<T>
        {
            private AVLTree<T> _container = null;
            private INode _currentPosition = null;
            private bool _isReset = true;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> class.
            /// </summary>
            /// <param name="container">The tree to enumerate.</param>
            public Enumerator(AVLTree<T> container)
            {
                this._container = container;
                Reset();
            }

            /// <summary>
            /// Advances the enumerator to the next element in the tree.
            /// </summary>
            /// <returns>True if the enumerator was successfully advanced to the next element; 
            /// false if the enumerator has passed the end of the tree.</returns>
            public bool MoveNext()
            {
                if (!_isReset && _currentPosition == null) return false;

                if (_isReset == true)
                    _currentPosition = _container._root != null ? _container._root.GetFarLeft() : null;
                else
                    _currentPosition = _currentPosition.GetSuccessor();

                _isReset = false;

                return _currentPosition != null;
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            void IDisposable.Dispose() { }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the tree.
            /// </summary>
            public void Reset() => _isReset = true;

            /// <summary>
            /// Gets the element in the tree at the current position of the enumerator.
            /// </summary>
            object IEnumerator.Current => Current;

            /// <summary>
            /// Gets the element in the tree at the current position of the enumerator.
            /// </summary>
            public T Current
            {
                get
                {
                    try
                    {
                        return _currentPosition.Value;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

        
}
*/
}