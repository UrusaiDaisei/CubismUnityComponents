using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Martinez
{
    /// <summary>
    /// Implementation of an AVL balanced binary search tree.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tree.</typeparam>
    public sealed partial class AVLTree<T>
    {
        private const int INITIAL_CAPACITY = 16;
        private struct NodeData
        {
            public T Value;
            public int ParentIndex;
            public int LeftIndex;
            public int RightIndex;
            public int Height;
        }

        private NodeData[] _nodes;
        private int _rootIndex = -1;
        private int _count = 0;
        private int _freeIndex = -1;
        private int _freeCount = 0;

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
            _count = _freeCount = 0;
            _nodes = new NodeData[INITIAL_CAPACITY];

            if (collection == null)
                return;

            foreach (var item in collection)
                Add(item);
        }

        /// <summary>
        /// Inserts an item into the tree.
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <returns>The node containing the inserted value.</returns>
        public INode Add(T value)
        {
            _rootIndex = Add(_rootIndex, value, out var result);
            return new NodeDataWrapper(this, result);
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
            _rootIndex = RemoveNode(_rootIndex, value, out var foundElement);
            return foundElement;
        }

        /// <summary>
        /// Removes the node at the specified position in the tree.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        /// <returns>True if the node was successfully removed; otherwise, false.</returns>
        public bool Remove(INode node2)
        {
            if (node2 == null)
                return false;

            var wrappedNode = (NodeDataWrapper)node2;
            ref var node = ref _nodes[wrappedNode.Index];

            if (node.LeftIndex == -1 || node.RightIndex == -1)
                return RemoveNodeWithFewerThanTwoChildren(wrappedNode.Index);

            var rightMin = GetFarLeft(node.RightIndex);
            Swap(wrappedNode.Index, rightMin);
            return Remove(wrappedNode);
        }

        /// <summary>
        /// Removes a node that has fewer than two children.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        /// <returns>True if the node was successfully removed.</returns>
        private bool RemoveNodeWithFewerThanTwoChildren(int nodeIndex)
        {
            ref var node = ref _nodes[nodeIndex];
            var parentIndex = node.ParentIndex;
            var wasLeft = parentIndex != -1 && _nodes[parentIndex].LeftIndex == nodeIndex;

            var left = new NodeDataManipulator(_nodes, node.LeftIndex);
            var right = new NodeDataManipulator(_nodes, node.RightIndex);

            var childNode = left.IsValid ? left : right;
            var otherNode = left.IsValid ? right : left;
            if (childNode.IsValid)
                childNode.Node.ParentIndex = parentIndex;

            if (parentIndex == -1)
                _rootIndex = childNode.Index;
            else if (wasLeft)
                _nodes[parentIndex].LeftIndex = childNode.Index;
            else
                _nodes[parentIndex].RightIndex = childNode.Index;

            DeallocateNode(otherNode.Index);
            RebalanceAfterRemoval(parentIndex);
            return true;
        }

        /// <summary>
        /// Rebalances the tree after node removal.
        /// </summary>
        /// <param name="startNode">The node to start rebalancing from.</param>
        private void RebalanceAfterRemoval(int startNodeIndex)
        {
            var target = startNodeIndex;
            while (target != -1)
            {
                BalanceBasedOnBalance(target);
                ref var node = ref _nodes[target];

                if (node.ParentIndex == -1)
                    _rootIndex = target;

                target = node.ParentIndex;
            }
        }

        /// <summary>
        /// Swaps two nodes in the tree, maintaining all links.
        /// </summary>
        /// <param name="a">First node to swap.</param>
        /// <param name="b">Second node to swap.</param>
        private void Swap(int aIndex, int bIndex)
        {
            if (aIndex == -1 || bIndex == -1)
                return;

            var a = new NodeDataManipulator(_nodes, aIndex);
            var b = new NodeDataManipulator(_nodes, bIndex);

            var aWasLeft = a.Parent.IsValid && a.Parent.Left == a;
            var bWasLeft = b.Parent.IsValid && b.Parent.Left == b;

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
        private void UpdateSwappedNodeReferences(NodeDataManipulator a, NodeDataManipulator b)
        {
            // if 'b' was the left node, right node or parent of 'a'
            if (b.Left == b)
                b.Left = a;
            else if (b.Right == b)
                b.Right = a;
            else if (b.Parent == b)
                b.Parent = a;

            // if 'a' was the left node, right node or parent of 'b'
            if (a.Left == a)
                a.Left = b;
            else if (a.Right == a)
                a.Right = b;
            else if (a.Parent == a)
                a.Parent = b;

            // Update child node parent references
            if (a.Left.IsValid)
                a.Left.Parent = a;
            if (a.Right.IsValid)
                a.Right.Parent = a;
            if (b.Left.IsValid)
                b.Left.Parent = b;
            if (b.Right.IsValid)
                b.Right.Parent = b;
        }

        /// <summary>
        /// Updates parent-child references after nodes are swapped.
        /// </summary>
        /// <param name="a">First swapped node.</param>
        /// <param name="aWasLeft">Whether first node was a left child.</param>
        /// <param name="b">Second swapped node.</param>
        /// <param name="bWasLeft">Whether second node was a left child.</param>
        private void UpdateParentChildReferences(NodeDataManipulator a, bool aWasLeft, NodeDataManipulator b, bool bWasLeft)
        {
            if (a.Parent.IsValid)
            {
                if (aWasLeft) a.Parent.Left = a;
                else a.Parent.Right = a;
            }
            else
            {
                _rootIndex = a.Index;
            }

            if (b.Parent.IsValid)
            {
                if (bWasLeft) b.Parent.Left = b;
                else b.Parent.Right = b;
            }
            else
            {
                _rootIndex = b.Index;
            }
        }

        /// <summary>
        /// Gets the node containing the minimum value in the tree.
        /// </summary>
        /// <returns>The node containing the minimum value, or null if the tree is empty.</returns>
        public INode GetMinNode() => _rootIndex != -1
            ? new NodeDataWrapper(this, GetFarLeft(_rootIndex))
            : null;

        public INode GetMaxNode() => _rootIndex != -1
            ? new NodeDataWrapper(this, GetFarRight(_rootIndex))
            : null;

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
            _nodes.AsSpan().Slice(0, _count)
                .Fill(new NodeData
                {
                    Value = default,
                    ParentIndex = -1,
                    LeftIndex = -1,
                    RightIndex = -1,
                    Height = 0
                });
            _rootIndex = -1;
            _freeIndex = -1;
            _count = 0;
            _freeCount = 0;
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
        public int Count => _count - _freeCount;

        /// <summary>
        /// Determines the height of a node in the tree.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>The height of the node, or 0 if the node is null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int DetermineHeight(int nodeIndex) => nodeIndex == -1 ? 0 : _nodes[nodeIndex].Height;

        /// <summary>
        /// Calculates the balance factor of a node in the tree.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>The balance factor (difference between left and right subtree heights).</returns>
        int CalculateBalance(int nodeIndex)
        {
            if (nodeIndex == -1)
                return 0;

            ref var node = ref _nodes[nodeIndex];
            return DetermineHeight(node.LeftIndex) - DetermineHeight(node.RightIndex);
        }

        /// <summary>
        /// Balances a node based on its balance factor.
        /// </summary>
        /// <param name="node">The node to balance.</param>
        /// <returns>The new root node after balancing.</returns>
        int BalanceBasedOnBalance(int nodeIndex)
        {
            if (nodeIndex == -1)
                return -1;

            ref var node = ref _nodes[nodeIndex];

            node.Height = Math.Max(DetermineHeight(node.LeftIndex), DetermineHeight(node.RightIndex)) + 1;

            int balance = CalculateBalance(nodeIndex);

            if (balance > 1 && CalculateBalance(node.LeftIndex) >= 0) return RotateRight(nodeIndex); // Left Left Case
            if (balance < -1 && CalculateBalance(node.RightIndex) <= 0) return RotateLeft(nodeIndex);  // Right Right Case

            if (balance > 1 && CalculateBalance(node.LeftIndex) < 0) // Left Right Case
            {
                node.LeftIndex = RotateLeft(node.LeftIndex);
                return RotateRight(nodeIndex);
            }

            if (balance < -1 && CalculateBalance(node.RightIndex) > 0) // Right Left Case
            {
                node.RightIndex = RotateRight(node.RightIndex);
                return RotateLeft(nodeIndex);
            }

            return nodeIndex;
        }

        /// <summary>
        /// Balances a node based on the value being inserted.
        /// </summary>
        /// <param name="node">The node to balance.</param>
        /// <param name="value">The value being inserted.</param>
        /// <returns>The new root node after balancing.</returns>
        int BalanceBasedOnValue(int index, T value)
        {
            if (index == -1)
                return -1;

            ref var node = ref _nodes[index];
            node.Height = Math.Max(DetermineHeight(node.LeftIndex), DetermineHeight(node.RightIndex)) + 1;

            int balance = CalculateBalance(index);

            if (balance > 1 && _comparer.Compare(value, _nodes[node.LeftIndex].Value) <= 0)
                return RotateRight(index); // Left Left Case
            if (balance < -1 && _comparer.Compare(value, _nodes[node.RightIndex].Value) >= 0)
                return RotateLeft(index);  // Right Right Case

            if (balance > 1 && _comparer.Compare(value, _nodes[node.LeftIndex].Value) > 0) // Left Right Case
            {
                node.LeftIndex = RotateLeft(node.LeftIndex);
                return RotateRight(index);
            }

            if (balance < -1 && _comparer.Compare(value, _nodes[node.RightIndex].Value) < 0) // Right Left Case
            {
                node.RightIndex = RotateRight(node.RightIndex);
                return RotateLeft(index);
            }

            return index;
        }

        /// <summary>
        /// Recursively adds a value to the tree.
        /// </summary>
        /// <param name="node">The current node in the recursive process.</param>
        /// <param name="value">The value to add.</param>
        /// <param name="result">Output parameter that will contain the newly created node.</param>
        /// <returns>The new root of the subtree after adding the value and balancing.</returns>
        int Add(int index, T value, out int result)
        {
            if (index == -1)
            {
                result = AllocateNode(value, index);
                return result;
            }

            ref var node = ref _nodes[index];

            if (_comparer.Compare(value, node.Value) < 0)
            {
                node.LeftIndex = Add(node.LeftIndex, value, out result);
                ref var leftNode = ref _nodes[node.LeftIndex];
                leftNode.ParentIndex = index;
            }
            else
            {
                node.RightIndex = Add(node.RightIndex, value, out result);
                ref var rightNode = ref _nodes[node.RightIndex];
                rightNode.ParentIndex = index;
            }

            return BalanceBasedOnValue(index, value);
        }

        int AllocateNode(T value, int parentIndex)
        {
            if (_freeIndex != -1)
            {
                var index = _freeIndex;
                _freeIndex = _nodes[index].ParentIndex;
                _nodes[index].Value = value;
                _nodes[index].ParentIndex = parentIndex;
                _freeCount--;
                return index;
            }

            if (_count == _nodes.Length)
                Array.Resize(ref _nodes, _nodes.Length * 2);

            _nodes[_count].Value = value;
            _nodes[_count].ParentIndex = parentIndex;
            _nodes[_count].LeftIndex = -1;
            _nodes[_count].RightIndex = -1;
            _nodes[_count].Height = 1;
            return _count++;
        }

        void DeallocateNode(int index)
        {
            _nodes[index] = new NodeData
            {
                Value = default,
                ParentIndex = _freeIndex,
                LeftIndex = -1,
                RightIndex = -1,
                Height = 0
            };
            _freeIndex = index;
            _freeCount++;
        }

        /// <summary>
        /// Recursively removes a value from the tree.
        /// </summary>
        /// <param name="node">The current node in the recursive process.</param>
        /// <param name="value">The value to remove.</param>
        /// <param name="wasFound">Output parameter that will be set to true if the value was found and removed.</param>
        /// <returns>The new root of the subtree after removing the value and balancing.</returns>
        private int RemoveNode(int nodeIndex, T value, out bool wasFound)
        {
            if (nodeIndex == -1)
            {
                wasFound = false;
                return -1;
            }

            ref var node = ref _nodes[nodeIndex];
            var compareResult = _comparer.Compare(value, node.Value);
            if (compareResult < 0)
                node.LeftIndex = RemoveNode(node.LeftIndex, value, out wasFound);
            else if (compareResult > 0)
                node.RightIndex = RemoveNode(node.RightIndex, value, out wasFound);
            else
            {
                if (node.LeftIndex == -1 || node.RightIndex == -1)
                {
                    var nodeToRemove = nodeIndex;
                    var oldParentIndex = node.ParentIndex;

                    if (node.LeftIndex == -1)
                        nodeIndex = node.RightIndex;
                    else
                        nodeIndex = node.LeftIndex;

                    if (nodeIndex != -1)
                        _nodes[nodeIndex].ParentIndex = oldParentIndex;

                    DeallocateNode(nodeToRemove);
                    wasFound = true;
                }
                else
                {
                    var rightMinIndex = GetFarLeft(node.RightIndex);
                    ref var rightMin = ref _nodes[rightMinIndex];
                    node.Value = rightMin.Value;
                    node.RightIndex = RemoveNode(node.RightIndex, rightMin.Value, out wasFound);
                }
            }

            return BalanceBasedOnBalance(nodeIndex);
        }

        private int GetFarLeft(int nodeIndex)
        {
            var result = nodeIndex;
            while (_nodes[result].LeftIndex != -1)
                result = _nodes[result].LeftIndex;

            return result;
        }

        private int GetFarRight(int nodeIndex)
        {
            var result = nodeIndex;

            while (_nodes[result].RightIndex != -1)
                result = _nodes[result].RightIndex;

            return result;
        }

        private int GetPredecessor(int nodeIndex)
        {
            ref var node = ref _nodes[nodeIndex];
            if (node.LeftIndex != -1)
                return GetFarRight(node.LeftIndex);

            var p = nodeIndex;
            while (_nodes[p].ParentIndex != -1 && _nodes[_nodes[p].ParentIndex].LeftIndex == p)
                p = _nodes[p].ParentIndex;

            return _nodes[p].ParentIndex;
        }

        private int GetSuccessor(int nodeIndex)
        {
            ref var node = ref _nodes[nodeIndex];
            if (node.RightIndex != -1)
                return GetFarLeft(node.RightIndex);

            var p = nodeIndex;
            while (_nodes[p].ParentIndex != -1 && _nodes[_nodes[p].ParentIndex].RightIndex == p)
                p = _nodes[p].ParentIndex;

            return _nodes[p].ParentIndex;
        }

        /// <summary>
        /// Finds a node with the specified value in the tree.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <returns>The node containing the value, or null if the value is not found.</returns>
        public INode Find(T value)
        {
            var current = _rootIndex;

            while (current != -1)
            {
                ref var node = ref _nodes[current];
                var compareResult = _comparer.Compare(value, node.Value);

                if (compareResult == 0)
                    return new NodeDataWrapper(this, current);

                current = (compareResult < 0)
                    ? node.LeftIndex
                    : node.RightIndex;
            }

            return null;
        }

        /// <summary>
        /// Performs a left rotation on the specified node.
        /// </summary>
        /// <param name="node">The node to rotate.</param>
        /// <returns>The new root node after rotation.</returns>
        int RotateLeft(int nodeIndex)
        {
            var node = new NodeDataManipulator(_nodes, nodeIndex);
            var right = node.Right;
            var rightLeft = right.Left;

            var parent = node.Parent;
            node.Right = rightLeft;

            if (rightLeft.IsValid)
                rightLeft.Parent = node;

            right.Left = node;
            node.Parent = right;

            if (parent.IsValid)
            {
                if (parent.Left == node)
                    parent.Left = right;
                else
                    parent.Right = right;
            }

            right.Parent = parent;

            node.Height = Math.Max(node.Left.Height, node.Right.Height) + 1;
            right.Height = Math.Max(right.Left.Height, right.Right.Height) + 1;

            return right.Index;
        }

        /// <summary>
        /// Performs a right rotation on the specified node.
        /// </summary>
        /// <param name="node">The node to rotate.</param>
        /// <returns>The new root node after rotation.</returns>
        int RotateRight(int nodeIndex)
        {
            var node = new NodeDataManipulator(_nodes, nodeIndex);
            var left = node.Left;
            var leftRight = left.Right;
            var parent = node.Parent;

            node.Left = leftRight;
            node.Parent = left;
            left.Parent = parent;
            left.Right = node;

            if (leftRight.IsValid)
                leftRight.Parent = node;

            if (parent.IsValid)
            {
                if (parent.Left == node)
                    parent.Left = left;
                else
                    parent.Right = left;
            }

            node.Height = Math.Max(node.Left.Height, node.Right.Height) + 1;
            left.Height = Math.Max(left.Left.Height, left.Right.Height) + 1;
            return left.Index;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the tree in order.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the tree.</returns>
        //public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        private class NodeDataManipulator
        {
            private readonly NodeData[] _nodes;
            public readonly int Index;

            public ref NodeData Node => ref _nodes[Index];

            public NodeDataManipulator(NodeData[] nodes, int index)
            {
                _nodes = nodes;
                Index = index;
            }

            public bool IsValid => Index != -1;

            public NodeDataManipulator Left
            {
                get
                {
                    return new NodeDataManipulator(_nodes, _nodes[Index].LeftIndex);
                }
                set
                {
                    _nodes[Index].LeftIndex = value.Index;
                }
            }

            public NodeDataManipulator Right
            {
                get
                {
                    return new NodeDataManipulator(_nodes, _nodes[Index].RightIndex);
                }
                set
                {
                    _nodes[Index].RightIndex = value.Index;
                }
            }

            public NodeDataManipulator Parent
            {
                get
                {
                    return new NodeDataManipulator(_nodes, _nodes[Index].ParentIndex);
                }
                set
                {
                    _nodes[Index].ParentIndex = value.Index;
                }
            }

            public int Height
            {
                get
                {
                    if (Index == -1)
                        return 0;

                    return _nodes[Index].Height;
                }
                set
                {
                    _nodes[Index].Height = value;
                }
            }

            public static bool operator ==(NodeDataManipulator a, NodeDataManipulator b)
            {
                return a.Index == b.Index;
            }

            public static bool operator !=(NodeDataManipulator a, NodeDataManipulator b)
            {
                return !(a == b);
            }
        }
    }
}


