using System.Collections.Generic;

/// <summary>
/// A minimum heap implementation where the smallest element is always at the root.
/// </summary>
/// <typeparam name="T">The type of elements in the heap.</typeparam>
public sealed class MinHeap<T>
{
    private readonly List<T> _stack;
    private readonly IComparer<T> _comparer;

    /// <summary>
    /// Gets the number of elements in the heap.
    /// </summary>
    public int Length => _stack.Count;

    /// <summary>
    /// Gets a value indicating whether the heap is empty.
    /// </summary>
    public bool IsEmpty => _stack.Count == 0;

    /// <summary>
    /// Removes all elements from the heap.
    /// </summary>
    public void Clear()
    {
        _stack.Clear();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinHeap{T}"/> class using the default comparer.
    /// </summary>
    public MinHeap() : this(Comparer<T>.Default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinHeap{T}"/> class using the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use for element comparisons.</param>
    public MinHeap(IComparer<T> comparer) : this(comparer, 16) // Default initial capacity
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinHeap{T}"/> class with the specified comparer and initial capacity.
    /// </summary>
    /// <param name="comparer">The comparer to use for element comparisons.</param>
    /// <param name="initialCapacity">The initial capacity of the heap.</param>
    public MinHeap(IComparer<T> comparer, int initialCapacity)
    {
        _stack = new List<T>(initialCapacity);
        _comparer = comparer;
    }

    /// <summary>
    /// Adds an element to the heap.
    /// </summary>
    /// <param name="value">The element to add.</param>
    public void Push(T value)
    {
        _stack.Add(value);
        BubbleUp(_stack.Count - 1);
    }

    /// <summary>
    /// Removes and returns the minimum element from the heap.
    /// </summary>
    /// <returns>The minimum element, or default(T) if the heap is empty.</returns>
    public T Pop()
    {
        if (IsEmpty)
            return default;

        var result = _stack[0];
        DeleteRoot();
        return result;
    }

    /// <summary>
    /// Returns the minimum element from the heap without removing it.
    /// </summary>
    /// <returns>The minimum element, or default(T) if the heap is empty.</returns>
    public T Peek()
    {
        return IsEmpty ? default : _stack[0];
    }

    /// <summary>
    /// Moves an element up in the heap until the heap property is satisfied.
    /// </summary>
    /// <param name="childIndex">The index of the element to move up.</param>
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

    /// <summary>
    /// Removes the root element from the heap and restores the heap property.
    /// </summary>
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

    /// <summary>
    /// Gets the left child of the element at the specified index.
    /// </summary>
    /// <param name="index">The index of the parent element.</param>
    /// <returns>The left child, or default(T) if there is no left child.</returns>
    public T Left(int index)
    {
        int leftChildIndex = index * 2 + 1;
        T result = default;
        if (_stack.Count > leftChildIndex)
            result = _stack[leftChildIndex];
        return result;
    }

    /// <summary>
    /// Gets the right child of the element at the specified index.
    /// </summary>
    /// <param name="index">The index of the parent element.</param>
    /// <returns>The right child, or default(T) if there is no right child.</returns>
    public T Right(int index)
    {
        int rightChildIndex = index * 2 + 2;
        T result = default;
        if (_stack.Count > rightChildIndex)
            result = _stack[rightChildIndex];
        return result;
    }

    /// <summary>
    /// Moves an element down in the heap until the heap property is satisfied.
    /// </summary>
    /// <param name="index">The index of the element to move down.</param>
    public void BubbleDown(int index)
    {
        var count = _stack.Count;

        while (true)
        {
            // On each iteration, exchange element with its smallest child
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

    /// <summary>
    /// Swaps two elements in the heap.
    /// </summary>
    /// <param name="pos1">The index of the first element.</param>
    /// <param name="pos2">The index of the second element.</param>
    public void Swap(int pos1, int pos2)
    {
        var temp = _stack[pos1];
        _stack[pos1] = _stack[pos2];
        _stack[pos2] = temp;
    }
}