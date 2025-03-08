using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Martinez
{
    /// <summary>
    /// Implementation of an AVL balanced binary search tree.
    /// </summary>
    /// <typeparam name="T">The type of elements in the tree.</typeparam>
    public class AVLTree<T> : ICollection<T>
    {
        /// <summary>
        /// Delegate for tree traversal operations.
        /// </summary>
        /// <param name="n">The node being visited.</param>
        public delegate void VisitHandler(AVLNode<T> n);

        /// <summary>
        /// The root node of the tree.
        /// </summary>
        private AVLNode<T> root;

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
            if (collection == null) throw new ArgumentNullException("collection");
            if (comparer == null) throw new ArgumentNullException("comparer");

            Comparer = comparer;

            if (collection != null)
            {
                foreach (T item in collection)
                {
                    this.Add(item);
                }
            }
        }

        /// <summary>
        /// Gets or sets the comparer used for element comparisons.
        /// </summary>
        protected IComparer<T> Comparer { get; set; }

        /// <summary>
        /// Adds an item to the tree.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <remarks>
        /// Complexity: O(log n)
        /// </remarks>
        public void Add(T value)
        {
            Insert(value);
        }

        /// <summary>
        /// Inserts an item into the tree.
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <returns>The node containing the inserted value.</returns>
        public AVLNode<T> Insert(T value)
        {
            AVLNode<T> result = null;

            root = Add(root, value, out result);

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
            bool foundElement = false;

            root = Remove(root, value, ref foundElement);

            return foundElement;
        }

        /// <summary>
        /// Removes the node at the specified position in the tree.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        /// <returns>True if the node was successfully removed; otherwise, false.</returns>
        public bool RemoveAt(AVLNode<T> node)
        {
            if (node == null) return false;

            if (node.Left == null || node.Right == null)
            {
                AVLNode<T> oldParent = node.Parent;

                bool nodeWasLeft = oldParent != null && oldParent.Left == node;

                AVLNode<T> target = node.Left != null ? node.Left : node.Right;

                if (oldParent != null && nodeWasLeft) oldParent.Left = target;
                else if (oldParent != null) oldParent.Right = target;

                if (target != null) target.Parent = oldParent;
                if (target == null) target = oldParent;
                if (target == null) root = null;

                while (target != null)
                {
                    BalanceBasedOnBalance(target);

                    if (target.Parent == null) root = target;

                    target = target.Parent;
                }

                return true;
            }
            else
            {
                AVLNode<T> rightMin = node.Right.GetFarLeft();

                Swap(node, rightMin);

                return RemoveAt(node);
            }
        }

        /// <summary>
        /// Swaps two nodes in the tree, maintaining all links.
        /// </summary>
        /// <param name="a">First node to swap.</param>
        /// <param name="b">Second node to swap.</param>
        void Swap(AVLNode<T> a, AVLNode<T> b)
        {
            if (a == null || b == null) return;

            bool aWasLeft = a.Parent != null && a.Parent.Left == a;
            bool bWasLeft = b.Parent != null && b.Parent.Left == b;

            AVLNode<T> tempLeft = a.Left;
            AVLNode<T> tempRight = a.Right;
            AVLNode<T> tempParent = a.Parent;
            int tempHeight = a.Height;

            a.Left = b.Left;
            a.Right = b.Right;
            a.Parent = b.Parent;
            a.Height = b.Height;

            b.Left = tempLeft;
            b.Right = tempRight;
            b.Parent = tempParent;
            b.Height = tempHeight;

            // if 'b' was the left node, right node or parent of 'a'
            if (b.Left == b) b.Left = a;
            else if (b.Right == b) b.Right = a;
            else if (b.Parent == b) b.Parent = a;

            // if 'a' was the left node, right node or parent of 'b'
            if (a.Left == a) a.Left = b;
            else if (a.Right == a) a.Right = b;
            else if (a.Parent == a) a.Parent = b;

            // swapping all pointers as in the following lines
            // keeps all AVLNodes the user might store valid

            if (a.Left != null) a.Left.Parent = a;
            if (a.Right != null) a.Right.Parent = a;

            if (b.Left != null) b.Left.Parent = b;
            if (b.Right != null) b.Right.Parent = b;

            if (a.Parent != null && bWasLeft) a.Parent.Left = a;
            else if (a.Parent != null) a.Parent.Right = a;

            if (b.Parent != null && aWasLeft) b.Parent.Left = b;
            else if (b.Parent != null) b.Parent.Right = b;
        }

        /// <summary>
        /// Gets the minimum value in the tree.
        /// </summary>
        /// <returns>The minimum value, or default(T) if the tree is empty.</returns>
        /// <remarks>
        /// Complexity: O(log n)
        /// </remarks>
        public T GetMin()
        {
            AVLNode<T> result = GetMinNode();

            return result != null ? result.Value : default(T);
        }

        /// <summary>
        /// Gets the node containing the minimum value in the tree.
        /// </summary>
        /// <returns>The node with the minimum value, or null if the tree is empty.</returns>
        public AVLNode<T> GetMinNode()
        {
            return root != null ? root.GetFarLeft() : null;
        }

        /// <summary>
        /// Gets the maximum value in the tree.
        /// </summary>
        /// <param name="value">When this method returns, contains the maximum value if found; otherwise, the default value for type T.</param>
        /// <returns>True if the maximum value was found; otherwise, false.</returns>
        /// <remarks>
        /// Complexity: O(log n)
        /// </remarks>
        public bool GetMax(out T value)
        {
            if (root != null)
            {
                value = root.GetFarRight().Value;

                return true;
            }

            value = default(T);

            return false;
        }

        /// <summary>
        /// Gets the value at the root of the tree.
        /// </summary>
        /// <returns>The value of the root node, or default(T) if the tree is empty.</returns>
        /// <remarks>
        /// Complexity: O(1)
        /// </remarks>
        public T GetRoot()
        {
            var value = default(T);
            if (root != null)
            {
                return root.Value;
            }
            return value;
        }

        /// <summary>
        /// Traverses the tree in-order, calling the specified visitor function for each node.
        /// </summary>
        /// <param name="visitor">The function to call for each node.</param>
        public void Traverse(VisitHandler visitor)
        {
            if (root != null && visitor != null) InOrder(root, visitor);
        }

        /// <summary>
        /// Returns a string representation of the tree structure.
        /// </summary>
        /// <returns>A formatted string showing the tree hierarchy.</returns>
        public override string ToString()
        {
            string result = "";

            List<List<AVLNode<T>>> childrenStack = new List<List<AVLNode<T>>>();
            childrenStack.Add(new List<AVLNode<T>> { root });

            while (childrenStack.Count > 0)
            {
                List<AVLNode<T>> childQueue = childrenStack[childrenStack.Count - 1];

                if (childQueue.Count == 0)
                {
                    childrenStack.RemoveAt(childrenStack.Count - 1);
                }
                else
                {
                    AVLNode<T> node = childQueue[0];
                    childQueue.RemoveAt(0);

                    string prefix = "";
                    for (int i = 0; i < childrenStack.Count - 1; ++i)
                    {
                        prefix += (childrenStack[i].Count > 0) ? "|  " : "   ";
                    }

                    string side = " ";

                    if (node.Parent != null) side = node.Parent.Left == node ? "L" : "R";

                    result += prefix + "+-" + side + " " + node.Value + "\n";

                    List<AVLNode<T>> children = new List<AVLNode<T>>();

                    if (node.Left != null) children.Add(node.Left);
                    if (node.Right != null) children.Add(node.Right);

                    if (children.Count > 0) childrenStack.Add(children);
                }
            }

            return result;
        }

        /// <summary>
        /// Performs an in-order traversal of the tree, calling the visitor function for each node.
        /// </summary>
        /// <param name="node">The node to start the traversal from.</param>
        /// <param name="visitor">The function to call for each node.</param>
        void InOrder(AVLNode<T> node, VisitHandler visitor)
        {
            if (node.Left != null) InOrder(node.Left, visitor);

            visitor(node);

            if (node.Right != null) InOrder(node.Right, visitor);
        }

        /// <summary>
        /// Performs a pre-order traversal of the tree, calling the visitor function for each node.
        /// </summary>
        /// <param name="node">The node to start the traversal from.</param>
        /// <param name="visitor">The function to call for each node.</param>
        void PreOrder(AVLNode<T> node, VisitHandler visitor)
        {
            visitor(node);

            if (node.Left != null) PreOrder(node.Left, visitor);
            if (node.Right != null) PreOrder(node.Right, visitor);
        }

        /// <summary>
        /// Performs a post-order traversal of the tree, calling the visitor function for each node.
        /// </summary>
        /// <param name="node">The node to start the traversal from.</param>
        /// <param name="visitor">The function to call for each node.</param>
        void PostOrder(AVLNode<T> node, VisitHandler visitor)
        {
            if (node.Left != null) PreOrder(node.Left, visitor);
            if (node.Right != null) PreOrder(node.Right, visitor);

            visitor(node);
        }

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
            root = null;
        }

        /// <summary>
        /// Gets a value indicating whether the tree is read-only.
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Gets the number of elements contained in the tree.
        /// </summary>
        /// <remarks>
        /// This is an expensive operation as it traverses the entire tree.
        /// </remarks>
        public int Count
        {
            get
            {
                int result = 0;

                Traverse((node) => { ++result; });

                return result;
            }
        }

        /// <summary>
        /// Copies the elements of the tree to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            IEnumerator<T> enumerator = GetEnumerator();

            while (enumerator.MoveNext())
            {
                array[arrayIndex++] = enumerator.Current;
            }
        }

        /// <summary>
        /// Determines the height of a node in the tree.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>The height of the node, or 0 if the node is null.</returns>
        static int DetermineHeight(AVLNode<T> node)
        {
            if (node == null) return 0;

            return node.Height;
        }

        /// <summary>
        /// Calculates the balance factor of a node in the tree.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>The balance factor (difference between left and right subtree heights).</returns>
        static int CalculateBalance(AVLNode<T> node)
        {
            if (node == null) return 0;

            return DetermineHeight(node.Left) - DetermineHeight(node.Right);
        }

        /// <summary>
        /// Balances a node based on its balance factor.
        /// </summary>
        /// <param name="node">The node to balance.</param>
        /// <returns>The new root node after balancing.</returns>
        AVLNode<T> BalanceBasedOnBalance(AVLNode<T> node)
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
        AVLNode<T> BalanceBasedOnValue(AVLNode<T> node, T value)
        {
            // if (node == null) return null;

            node.Height = Math.Max(DetermineHeight(node.Left), DetermineHeight(node.Right)) + 1;

            int balance = CalculateBalance(node);

            if (balance > 1 && Comparer.Compare(value, node.Left.Value) <= 0) return RotateRight(node); // Left Left Case
            if (balance < -1 && Comparer.Compare(value, node.Right.Value) >= 0) return RotateLeft(node);  // Right Right Case

            if (balance > 1 && Comparer.Compare(value, node.Left.Value) > 0) // Left Right Case
            {
                node.Left = RotateLeft(node.Left);
                return RotateRight(node);
            }

            if (balance < -1 && Comparer.Compare(value, node.Right.Value) < 0) // Right Left Case
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
        void UpdateHeight(AVLNode<T> node, AVLNode<T> parent)
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
        AVLNode<T> Add(AVLNode<T> node, T value, out AVLNode<T> result)
        {
            if (node == null)
            {
                result = new AVLNode<T>(value);
                return result;
            }

            if (Comparer.Compare(value, node.Value) < 0)
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
        AVLNode<T> Remove(AVLNode<T> node, T value, ref bool wasFound)
        {
            if (node == null) return null;

            if (Comparer.Compare(value, node.Value) < 0) node.Left = Remove(node.Left, value, ref wasFound);
            else if (Comparer.Compare(value, node.Value) > 0) node.Right = Remove(node.Right, value, ref wasFound);
            else
            {
                if (node.Left == null || node.Right == null)
                {
                    AVLNode<T> oldParent = node.Parent;

                    if (node.Left == null) node = node.Right;
                    else node = node.Left;

                    if (node != null) node.Parent = oldParent;

                    wasFound = true;
                }
                else
                {
                    AVLNode<T> rightMin = node.Right.GetFarLeft();
                    node.Value = rightMin.Value;
                    node.Right = Remove(node.Right, rightMin.Value, ref wasFound);
                }
            }

            return BalanceBasedOnBalance(node);
        }

        /// <summary>
        /// Finds a node with the specified value in the tree.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <returns>The node containing the value, or null if the value is not found.</returns>
        public AVLNode<T> Find(T value)
        {
            AVLNode<T> current = root;

            while (current != null)
            {
                if (Comparer.Compare(value, current.Value) < 0) current = current.Left;
                else if (Comparer.Compare(value, current.Value) > 0) current = current.Right;
                else return current;
            }

            return null;
        }

        /// <summary>
        /// Performs a left rotation on the specified node.
        /// </summary>
        /// <param name="node">The node to rotate.</param>
        /// <returns>The new root node after rotation.</returns>
        static AVLNode<T> RotateLeft(AVLNode<T> node)
        {
            AVLNode<T> right = node.Right;
            AVLNode<T> rightLeft = right.Left;
            node.Right = rightLeft;

            AVLNode<T> parent = node.Parent;

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
        static AVLNode<T> RotateRight(AVLNode<T> node)
        {
            AVLNode<T> left = node.Left;
            AVLNode<T> leftRight = left.Right;
            AVLNode<T> parent = node.Parent;

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
        public IEnumerator<T> GetEnumerator() { return new AVLNodeEnumerator(this); }

        /// <summary>
        /// Returns an enumerator that iterates through the tree in order.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the tree.</returns>
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        /// <summary>
        /// Enumerator class for traversing the AVL tree in-order.
        /// </summary>
        class AVLNodeEnumerator : IEnumerator<T>
        {
            private AVLTree<T> container = null;
            private AVLNode<T> currentPosition = null;
            private bool isReset = true;

            /// <summary>
            /// Initializes a new instance of the <see cref="AVLNodeEnumerator"/> class.
            /// </summary>
            /// <param name="container">The tree to enumerate.</param>
            public AVLNodeEnumerator(AVLTree<T> container)
            {
                this.container = container;
                Reset();
            }

            /// <summary>
            /// Advances the enumerator to the next element in the tree.
            /// </summary>
            /// <returns>True if the enumerator was successfully advanced to the next element; 
            /// false if the enumerator has passed the end of the tree.</returns>
            public bool MoveNext()
            {
                if (!isReset && currentPosition == null) return false;

                if (isReset == true) currentPosition = container.root != null ? container.root.GetFarLeft() : null;
                else currentPosition = currentPosition.GetSuccessor();

                isReset = false;

                return (currentPosition != null);
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            void IDisposable.Dispose() { }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the tree.
            /// </summary>
            public void Reset()
            {
                isReset = true;
            }

            /// <summary>
            /// Gets the element in the tree at the current position of the enumerator.
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            /// <summary>
            /// Gets the element in the tree at the current position of the enumerator.
            /// </summary>
            public T Current
            {
                get
                {
                    try
                    {
                        return currentPosition.Value;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }
    }
}


