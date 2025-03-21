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

        public struct NodeDataWrapper : INode
        {
            private readonly AVLTree<T> _tree;
            public readonly int Index;

            public NodeDataWrapper(AVLTree<T> tree, int index)
            {
                _tree = tree;
                Index = index;
            }

            public T Value => _tree._nodes[Index].Value;

            public INode GetPredecessor()
            {
                var index = _tree.GetPredecessor(Index);
                return index == -1 ? null : new NodeDataWrapper(_tree, index);
            }

            public INode GetSuccessor()
            {
                var index = _tree.GetSuccessor(Index);
                return index == -1 ? null : new NodeDataWrapper(_tree, index);
            }

        }
    }
}