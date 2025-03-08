using System.Collections.Generic;

public sealed class MinHeap<T>
{
    private readonly List<T> _stack;
    private readonly IComparer<T> _comparer;

    public int Length => _stack.Count;
    public bool IsEmpty => _stack.Count == 0;

    public void Clear()
    {
        _stack.Clear();
    }

    public MinHeap() : this(Comparer<T>.Default) { }

    public MinHeap(IComparer<T> comparer) : this(comparer, 16) // Default initial capacity
    {
    }

    public MinHeap(IComparer<T> comparer, int initialCapacity)
    {
        _stack = new List<T>(initialCapacity);
        _comparer = comparer;
    }

    public void Push(T value)
    {
        _stack.Add(value);
        BubbleUp(_stack.Count - 1);
    }

    public T Pop()
    {
        if (IsEmpty)
            return default;

        var result = _stack[0];
        DeleteRoot();
        return result;
    }

    public T Peek()
    {
        return IsEmpty ? default : _stack[0];
    }

    public void BubbleUp(int childIndex)
    {
        while (childIndex > 0)
        {
            var parentIndex = (childIndex - 1) / 2; // Parent index calculation
            if (_comparer.Compare(_stack[childIndex], _stack[parentIndex]) < 0)
            {
                Swap(parentIndex, childIndex);
                childIndex = parentIndex;
            }
            else break;
        }
    }

    public void DeleteRoot()
    {
        if (_stack.Count <= 1)
        {
            _stack.Clear();
            return;
        }

        Swap(0, _stack.Count - 1);
        _stack.RemoveAt(_stack.Count - 1);   // Move the last element to the root and remove the last position
        BubbleDown(0);
    }
    public T Left(int index)
    {
        int leftChildIndex = index * 2 + 1;
        T result = default;
        if (_stack.Count > leftChildIndex)
            result = _stack[leftChildIndex];
        return result;
    }
    public T Right(int index)
    {
        int rightChildIndex = index * 2 + 2;
        T result = default;
        if (_stack.Count > rightChildIndex)
            result = _stack[rightChildIndex];
        return result;
    }
    public void BubbleDown(int index)
    {
        var count = _stack.Count;

        while (true)
        {
            // on each iteration exchange element with its smallest child
            int leftChildIndex = index * 2 + 1;
            int rightChildIndex = index * 2 + 2;
            int smallestItemIndex = index; // The index of the parent

            if (leftChildIndex < count && _comparer.Compare(_stack[leftChildIndex], _stack[smallestItemIndex]) < 0)
                smallestItemIndex = leftChildIndex;

            if (rightChildIndex < count && _comparer.Compare(_stack[rightChildIndex], _stack[smallestItemIndex]) < 0)
                smallestItemIndex = rightChildIndex;

            if (smallestItemIndex == index)
                break;

            Swap(smallestItemIndex, index);
            index = smallestItemIndex;
        }
    }

    public void Swap(int pos1, int pos2)
    {
        var temp = _stack[pos1];
        _stack[pos1] = _stack[pos2];
        _stack[pos2] = temp;
    }
}