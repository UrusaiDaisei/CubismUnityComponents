using System;
using System.Buffers;
using System.Collections.Generic;

namespace Live2D.Cubism.Framework.Utils
{
    public static class SpanExtentions
    {
        public static void RightShift<T>(this Span<T> span, int index, int amount)
        {
            if (amount <= 0)
                return;

            // When shifting right, copy from the part that needs to be moved
            // The length of the copied section must be adjusted to avoid exceeding the span
            int copyLength = Math.Max(0, span.Length - index - amount);
            if (copyLength <= 0)
                return;

            var source = span.Slice(index, copyLength);
            var destination = span.Slice(index + amount, copyLength);
            source.CopyTo(destination);
        }

        public static Span<T> OrderBy<T, V>(this Span<T> span, Func<T, V> selector)
            => OrderBy(span, selector, Comparer<V>.Default);

        public static Span<T> OrderBy<T, V>(this Span<T> span, Func<T, V> selector, IComparer<V> comparer)
        {
            Sort(span, (a, b) => comparer.Compare(selector(a), selector(b)));
            return span;
        }

        public static void Sort<T>(this Span<T> span, Comparison<T> comparison)
        {
            if (span.Length <= 1)
                return;

            // Use insertion sort for small arrays (faster for small n)
            if (span.Length <= 16)
            {
                InsertionSort(span, comparison);
                return;
            }

            T[] tempArray = ArrayPool<T>.Shared.Rent(span.Length);
            MergeSort(span, tempArray.AsSpan(0, span.Length), comparison);
            ArrayPool<T>.Shared.Return(tempArray);

            static void InsertionSort(Span<T> array, Comparison<T> comparison)
            {
                for (int i = 1; i < array.Length; i++)
                {
                    T key = array[i];
                    int j = i - 1;

                    while (j >= 0 && comparison(array[j], key) > 0)
                    {
                        array[j + 1] = array[j];
                        j--;
                    }
                    array[j + 1] = key;
                }
            }

            static void MergeSort(Span<T> source, Span<T> temp, Comparison<T> comparison)
            {
                // Bottom-up merge sort implementation (non-recursive)
                for (int width = 1; width < source.Length; width *= 2)
                {
                    // Process the array in chunks of increasing width
                    for (int i = 0; i < source.Length; i += 2 * width)
                    {
                        // Define the bounds of the two subarrays to merge
                        int left = i;
                        int mid = Math.Min(i + width, source.Length);
                        int right = Math.Min(i + 2 * width, source.Length);

                        // Merge the two subarrays
                        Merge(source, temp, left, mid, right, comparison);
                    }
                }
            }

            static void Merge(Span<T> source, Span<T> temp, int left, int mid, int right, Comparison<T> comparison)
            {
                // Copy the entire segment to temp first
                source.Slice(left, right - left).CopyTo(temp.Slice(left));

                int leftIdx = left;
                int rightIdx = mid;
                int sourceIdx = left;

                // Merge back from temp to source directly
                while (leftIdx < mid && rightIdx < right)
                {
                    if (comparison(temp[leftIdx], temp[rightIdx]) <= 0)
                    {
                        source[sourceIdx++] = temp[leftIdx++];
                    }
                    else
                    {
                        source[sourceIdx++] = temp[rightIdx++];
                    }
                }

                // Copy any remaining elements from left subarray
                // Only one of these two loops will execute
                while (leftIdx < mid)
                {
                    source[sourceIdx++] = temp[leftIdx++];
                }

                // No need to copy from right subarray since they're already in the correct position
                // The right side elements are already in their correct positions
            }
        }
    }
}