using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Martinez
{
    /// <summary>
    /// Implementation of an AVL balanced binary search tree.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tree.</typeparam>
    public sealed partial class AVLTree<T> : ICollection<T>
    {
        /// <summary>
        /// The root node of the tree.
        /// </summary>
        private Node _root;

        /// <summary>
        /// Comparer used for element comparisons.
        /// </summary>
        private readonly IComparer<T> _comparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AVLTree{T}"/> class
        /// using the default comparer.
        /// </summary>
        public AVLTree() : this(Comparer<T>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AVLTree{T}"/> class
        /// using the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        public AVLTree(IComparer<T> comparer) : this(Enumerable.Empty<T>(), comparer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AVLTree{T}"/> class
        /// with elements from the specified collection, using the default comparer.
        /// </summary>
        /// <param name="collection">The collection whose elements will be added to the tree.</param>
        public AVLTree(IEnumerable<T> collection) : this(collection, Comparer<T>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AVLTree{T}"/> class
        /// with elements from the specified collection, using the specified comparer.
        /// </summary>
        /// <param name="collection">The collection whose elements will be added to the tree.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        public AVLTree(IEnumerable<T> collection, IComparer<T> comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            _comparer = comparer;
            Count = 0; // Initialize count to 0

            if (collection == null)
                return;

            foreach (var item in collection)
                Add(item);
        }

        /// <summary>
        /// Adds the specified item to the tree.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <remarks>
        /// Complexity: O(log n)
        /// </remarks>
        void ICollection<T>.Add(T value) => Add(value);

        /// <summary>
        /// Inserts an item into the tree.
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <returns>The node containing the inserted value.</returns>
        public INode Add(T value)
        {
            _root = Add(_root, value, out var result);
            Count++; // Increment count when adding a node
            return result;
        }

        /// <summary>
        /// Removes a specified item from the tree.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        /// <returns>True if the item was found and removed; otherwise, false.</returns>
        /// <remarks>
        /// Complexity: O(log n)
        /// </remarks>
        public bool Remove(T value)
        {
            _root = RemoveNode(_root, value, out var foundElement);
            if (foundElement)
                Count--; // Decrement count when a node is removed
            return foundElement;
        }

        /// <summary>
        /// Removes the node at the specified position in the tree.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        /// <returns>True if the node was successfully removed; otherwise, false.</returns>
        public bool Remove(INode node)
        {
            if (node == null)
                return false;

            var wrappedNode = (Node)node;

            if (wrappedNode.Left == null || wrappedNode.Right == null)
                return RemoveNodeWithFewerThanTwoChildren(wrappedNode);

            var rightMin = wrappedNode.Right.GetFarLeft();
            Swap(wrappedNode, rightMin);
            return Remove(wrappedNode);
        }

        /// <summary>
        /// Removes a node that has fewer than two children.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        /// <returns>True if the node was successfully removed.</returns>
        private bool RemoveNodeWithFewerThanTwoChildren(Node node)
        {
            var parent = node.Parent;
            var wasLeft = parent != null && ReferenceEquals(parent.Left, node);

            var childNode = node.Left ?? node.Right;
            if (childNode != null)
                childNode.Parent = parent;

            if (parent == null)
                _root = childNode;
            else if (wasLeft)
                parent.Left = childNode;
            else
                parent.Right = childNode;

            RebalanceAfterRemoval(parent);
            Count--; // Decrement count when a node is removed
            return true;
        }

        /// <summary>
        /// Rebalances the tree after node removal.
        /// </summary>
        /// <param name="startNode">The node to start rebalancing from.</param>
        private void RebalanceAfterRemoval(Node startNode)
        {
            var target = startNode;
            while (target != null)
            {
                BalanceBasedOnBalance(target);

                if (target.Parent == null) _root = target;

                target = target.Parent;
            }
        }

        /// <summary>
        /// Swaps two nodes in the tree, maintaining all links.
        /// </summary>
        /// <param name="a">First node to swap.</param>
        /// <param name="b">Second node to swap.</param>
        private void Swap(Node a, Node b)
        {
            if (a == null || b == null) return;

            var aWasLeft = a.Parent != null && a.Parent.Left == a;
            var bWasLeft = b.Parent != null && b.Parent.Left == b;

            var tempLeft = a.Left;
            var tempRight = a.Right;
            var tempParent = a.Parent;
            var tempHeight = a.Height;

            a.Left = b.Left;
            a.Right = b.Right;
            a.Parent = b.Parent;
            a.Height = b.Height;

            b.Left = tempLeft;
            b.Right = tempRight;
            b.Parent = tempParent;
            b.Height = tempHeight;

            UpdateSwappedNodeReferences(a, b);
            UpdateParentChildReferences(a, aWasLeft, b, bWasLeft);
        }

        /// <summary>
        /// Updates references for nodes that were swapped.
        /// </summary>
        /// <param name="a">First swapped node.</param>
        /// <param name="b">Second swapped node.</param>
        private void UpdateSwappedNodeReferences(Node a, Node b)
        {
            // if 'b' was the left node, right node or parent of 'a'
            if (b.Left == b) b.Left = a;
            else if (b.Right == b) b.Right = a;
            else if (b.Parent == b) b.Parent = a;

            // if 'a' was the left node, right node or parent of 'b'
            if (a.Left == a) a.Left = b;
            else if (a.Right == a) a.Right = b;
            else if (a.Parent == a) a.Parent = b;

            // Update child node parent references
            if (a.Left != null) a.Left.Parent = a;
            if (a.Right != null) a.Right.Parent = a;
            if (b.Left != null) b.Left.Parent = b;
            if (b.Right != null) b.Right.Parent = b;
        }

        /// <summary>
        /// Updates parent-child references after nodes are swapped.
        /// </summary>
        /// <param name="a">First swapped node.</param>
        /// <param name="aWasLeft">Whether first node was a left child.</param>
        /// <param name="b">Second swapped node.</param>
        /// <param name="bWasLeft">Whether second node was a left child.</param>
        private void UpdateParentChildReferences(Node a, bool aWasLeft, Node b, bool bWasLeft)
        {
            if (a.Parent != null)
            {
                if (aWasLeft) a.Parent.Left = a;
                else a.Parent.Right = a;
            }
            else
            {
                _root = a;
            }

            if (b.Parent != null)
            {
                if (bWasLeft) b.Parent.Left = b;
                else b.Parent.Right = b;
            }
            else
            {
                _root = b;
            }
        }

        /// <summary>
        /// Gets the minimum value in the tree.
        /// </summary>
        /// <returns>The minimum value in the tree.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the tree is empty.</exception>
        public T GetMin()
        {
            if (_root == null)
                throw new InvalidOperationException("Tree is empty");
            return GetMinNode().Value;
        }

        /// <summary>
        /// Gets the node containing the minimum value in the tree.
        /// </summary>
        /// <returns>The node containing the minimum value, or null if the tree is empty.</returns>
        public INode GetMinNode() => _root != null ? _root.GetFarLeft() : null;

        /// <summary>
        /// Gets the maximum value in the tree.
        /// </summary>
        /// <returns>The maximum value, or the default value if the tree is empty.</returns>
        public T GetMax()
        {
            if (_root == null)
                throw new InvalidOperationException("Tree is empty");
            return GetMaxNode().Value;
        }

        public INode GetMaxNode() => _root != null ? _root.GetFarRight() : null;

        /// <summary>
        /// Determines whether the tree contains a specific value.
        /// </summary>
        /// <param name="arg">The value to locate in the tree.</param>
        /// <returns>True if the tree contains the specified value; otherwise, false.</returns>
        /// <remarks>
        /// Complexity: O(log n)
        /// </remarks>
        public bool Contains(T arg)
        {
            return Find(arg) != null;
        }

        /// <summary>
        /// Removes all elements from the tree.
        /// </summary>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        public void Clear()
        {
            _root = null;
            Count = 0; // Reset count when clearing the tree
        }

        /// <summary>
        /// Gets a value indicating whether the tree is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets the number of elements contained in the tree.
        /// </summary>
        /// <remarks>
        /// This is now an O(1) operation.
        /// </remarks>
        public int Count
        {
            get; private set;
        }

        /// <summary>
        /// Copies the elements of the tree to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentException("Destination array is not long enough to copy all the items in the collection.");

            var i = arrayIndex;
            foreach (var item in this)
            {
                array[i++] = item;
            }
        }

        /// <summary>
        /// Determines the height of a node in the tree.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>The height of the node, or 0 if the node is null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int DetermineHeight(Node node) => node?.Height ?? 0;

        /// <summary>
        /// Calculates the balance factor of a node in the tree.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>The balance factor (difference between left and right subtree heights).</returns>
        static int CalculateBalance(Node node)
        {
            if (node == null) return 0;

            return DetermineHeight(node.Left) - DetermineHeight(node.Right);
        }

        /// <summary>
        /// Balances a node based on its balance factor.
        /// </summary>
        /// <param name="node">The node to balance.</param>
        /// <returns>The new root node after balancing.</returns>
        Node BalanceBasedOnBalance(Node node)
        {
            if (node == null) return null;

            node.Height = Math.Max(DetermineHeight(node.Left), DetermineHeight(node.Right)) + 1;

            int balance = CalculateBalance(node);

            if (balance > 1 && CalculateBalance(node.Left) >= 0) return RotateRight(node); // Left Left Case
            if (balance < -1 && CalculateBalance(node.Right) <= 0) return RotateLeft(node);  // Right Right Case

            if (balance > 1 && CalculateBalance(node.Left) < 0) // Left Right Case
            {
                node.Left = RotateLeft(node.Left);
                return RotateRight(node);
            }

            if (balance < -1 && CalculateBalance(node.Right) > 0) // Right Left Case
            {
                node.Right = RotateRight(node.Right);
                return RotateLeft(node);
            }

            return node;
        }

        /// <summary>
        /// Balances a node based on the value being inserted.
        /// </summary>
        /// <param name="node">The node to balance.</param>
        /// <param name="value">The value being inserted.</param>
        /// <returns>The new root node after balancing.</returns>
        Node BalanceBasedOnValue(Node node, T value)
        {
            // if (node == null) return null;

            node.Height = Math.Max(DetermineHeight(node.Left), DetermineHeight(node.Right)) + 1;

            int balance = CalculateBalance(node);

            if (balance > 1 && _comparer.Compare(value, node.Left.Value) <= 0) return RotateRight(node); // Left Left Case
            if (balance < -1 && _comparer.Compare(value, node.Right.Value) >= 0) return RotateLeft(node);  // Right Right Case

            if (balance > 1 && _comparer.Compare(value, node.Left.Value) > 0) // Left Right Case
            {
                node.Left = RotateLeft(node.Left);
                return RotateRight(node);
            }

            if (balance < -1 && _comparer.Compare(value, node.Right.Value) < 0) // Right Left Case
            {
                node.Right = RotateRight(node.Right);
                return RotateLeft(node);
            }

            return node;
        }

        /// <summary>
        /// Updates the height of a node and all its ancestors up to a specified parent.
        /// </summary>
        /// <param name="node">The node to start updating from.</param>
        /// <param name="parent">The parent node to stop at (not including).</param>
        void UpdateHeight(Node node, Node parent)
        {
            if (node != parent)
            {
                node.Height = Math.Max(DetermineHeight(node.Left), DetermineHeight(node.Right)) + 1;

                UpdateHeight(node.Parent, parent);
            }
        }

        /// <summary>
        /// Recursively adds a value to the tree.
        /// </summary>
        /// <param name="node">The current node in the recursive process.</param>
        /// <param name="value">The value to add.</param>
        /// <param name="result">Output parameter that will contain the newly created node.</param>
        /// <returns>The new root of the subtree after adding the value and balancing.</returns>
        Node Add(Node node, T value, out Node result)
        {
            if (node == null)
            {
                result = new Node(value);
                return result;
            }

            if (_comparer.Compare(value, node.Value) < 0)
            {
                node.Left = Add(node.Left, value, out result);
                node.Left.Parent = node;
            }
            else
            {
                node.Right = Add(node.Right, value, out result);
                node.Right.Parent = node;
            }

            return BalanceBasedOnValue(node, value);
        }

        /// <summary>
        /// Recursively removes a value from the tree.
        /// </summary>
        /// <param name="node">The current node in the recursive process.</param>
        /// <param name="value">The value to remove.</param>
        /// <param name="wasFound">Output parameter that will be set to true if the value was found and removed.</param>
        /// <returns>The new root of the subtree after removing the value and balancing.</returns>
        private Node RemoveNode(Node node, T value, out bool wasFound)
        {
            if (node == null)
            {
                wasFound = false;
                return null;
            }

            var compareResult = _comparer.Compare(value, node.Value);
            if (compareResult < 0)
                node.Left = RemoveNode(node.Left, value, out wasFound);
            else if (compareResult > 0)
                node.Right = RemoveNode(node.Right, value, out wasFound);
            else
            {
                if (node.Left == null || node.Right == null)
                {
                    Node oldParent = node.Parent;

                    if (node.Left == null)
                        node = node.Right;
                    else
                        node = node.Left;

                    if (node != null)
                        node.Parent = oldParent;

                    wasFound = true;
                }
                else
                {
                    Node rightMin = node.Right.GetFarLeft();
                    node.Value = rightMin.Value;
                    node.Right = RemoveNode(node.Right, rightMin.Value, out wasFound);
                }
            }

            return BalanceBasedOnBalance(node);
        }

        /// <summary>
        /// Finds a node with the specified value in the tree.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <returns>The node containing the value, or null if the value is not found.</returns>
        public INode Find(T value)
        {
            var current = _root;

            while (current != null)
            {
                var compareResult = _comparer.Compare(value, current.Value);

                if (compareResult == 0)
                    return current;

                current = (compareResult < 0)
                    ? current.Left
                    : current.Right;
            }

            return null;
        }

        /// <summary>
        /// Performs a left rotation on the specified node.
        /// </summary>
        /// <param name="node">The node to rotate.</param>
        /// <returns>The new root node after rotation.</returns>
        static Node RotateLeft(Node node)
        {
            Node right = node.Right;
            Node rightLeft = right.Left;
            node.Right = rightLeft;

            Node parent = node.Parent;

            if (rightLeft != null) rightLeft.Parent = node;

            right.Left = node;
            node.Parent = right;

            if (parent != null)
            {
                if (parent.Left == node) parent.Left = right;
                else parent.Right = right;
            }

            right.Parent = parent;

            node.Height = Math.Max(DetermineHeight(node.Left), DetermineHeight(node.Right)) + 1;
            right.Height = Math.Max(DetermineHeight(right.Left), DetermineHeight(right.Right)) + 1;

            return right;
        }

        /// <summary>
        /// Performs a right rotation on the specified node.
        /// </summary>
        /// <param name="node">The node to rotate.</param>
        /// <returns>The new root node after rotation.</returns>
        static Node RotateRight(Node node)
        {
            Node left = node.Left;
            Node leftRight = left.Right;
            Node parent = node.Parent;

            node.Left = leftRight;
            node.Parent = left;
            left.Parent = parent;
            left.Right = node;

            if (leftRight != null) leftRight.Parent = node;

            if (parent != null)
            {
                if (parent.Left == node) parent.Left = left;
                else parent.Right = left;
            }

            node.Height = Math.Max(DetermineHeight(node.Left), DetermineHeight(node.Right)) + 1;
            left.Height = Math.Max(DetermineHeight(left.Left), DetermineHeight(left.Right)) + 1;

            return left;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the tree in order.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the tree.</returns>
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Returns an enumerator that iterates through the tree in order.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the tree.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}


