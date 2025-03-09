namespace Martinez
{
    partial class AVLTree<T>
    {
        public interface INode
        {
            T Value { get; }

            INode GetPredecessor();
            INode GetSuccessor();
        }

        /// <summary>
        /// Represents a node in an AVL balanced binary search tree.
        /// </summary>
        /// <typeparam name="T">The type of value stored in the node.</typeparam>
        private sealed class Node : INode
        {
            /// <summary>
            /// Gets or sets the left child node.
            /// </summary>
            public Node Left { get; set; }

            /// <summary>
            /// Gets or sets the right child node.
            /// </summary>
            public Node Right { get; set; }

            /// <summary>
            /// Gets or sets the parent node.
            /// </summary>
            public Node Parent { get; set; }

            /// <summary>
            /// Gets or sets the value stored in this node.
            /// </summary>
            public T Value { get; set; }

            /// <summary>
            /// Gets or sets the height of this node in the tree.
            /// </summary>
            public int Height { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Node"/> class.
            /// </summary>
            /// <param name="value">The value to store in this node.</param>
            public Node(T value)
            {
                this.Value = value;

                this.Parent = null;
                this.Left = null;
                this.Right = null;

                this.Height = 1;
            }

            /// <summary>
            /// Gets the predecessor node (the node with the greatest value that's smaller than this node's value).
            /// </summary>
            /// <returns>The predecessor node, or null if there is no predecessor.</returns>
            public INode GetPredecessor()
            {
                if (Left != null)
                    return Left.GetFarRight();

                var p = this;

                while (p.Parent != null && p.Parent.Left == p)
                    p = p.Parent;

                return p.Parent;
            }

            /// <summary>
            /// Gets the successor node (the node with the smallest value that's greater than this node's value).
            /// </summary>
            /// <returns>The successor node, or null if there is no successor.</returns>
            public INode GetSuccessor()
            {
                if (Right != null)
                    return Right.GetFarLeft();

                var p = this;
                while (p.Parent != null && p.Parent.Right == p)
                    p = p.Parent;
                return p.Parent;
            }

            /// <summary>
            /// Gets the leftmost descendant of this node.
            /// </summary>
            /// <returns>The leftmost node in the subtree rooted at this node.</returns>
            public Node GetFarLeft()
            {
                var result = this;

                while (result.Left != null)
                {
                    result = result.Left;
                }

                return result;
            }

            /// <summary>
            /// Gets the rightmost descendant of this node.
            /// </summary>
            /// <returns>The rightmost node in the subtree rooted at this node.</returns>
            public Node GetFarRight()
            {
                var result = this;

                while (result.Right != null)
                {
                    result = result.Right;
                }

                return result;
            }
        }
    }
}