
using System;
using System.Collections;

namespace Bonsai.Utility
{

  /// <summary>
  /// A specialized sorting helper for arrays whose length stay constant but need to be sorted continuously.
  /// It uses insertion sort for small arrays and quick sort for larger arrays.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class FixedSorter<T> where T : IComparable<T>
  {
    private readonly FixedSizeStack<int> _quickSortStack;
    private readonly T[] _arrayToSort;
    private const int kLengthHeuristic = 16;

    public FixedSorter(T[] array)
    {
      _arrayToSort = array;
      if (array.Length > kLengthHeuristic)
      {
        _quickSortStack = new FixedSizeStack<int>(array.Length);
      }
    }

    public void Sort()
    {
      if (_arrayToSort.Length <= kLengthHeuristic)
      {
        Sorts.InsertionSort(_arrayToSort);
      }
      else
      {
        Sorts.QuickSort(_quickSortStack, _arrayToSort);
      }
    }
  }

  public static class Sorts
  {
    public static void InsertionSort<T>(T[] array) where T : IComparable<T>
    {
      for (var i = 1; i < array.Length; i++)
      {
        for (var j = i; j > 0; j--)
        {
          if (array[j - 1].CompareTo(array[j]) > 0)
          {
            swap(ref array[j], ref array[j - 1]);
          }
        }
      }
    }

    public static void QuickSort<T>(T[] array) where T : IComparable<T>
    {
      QuickSort<T>(new FixedSizeStack<int>(array.Length), array);
    }

    /// <summary>
    /// An iterative quick sort.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stack">The stack memory for sorting. The length must match the input array length</param>
    /// <param name="array"></param>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    public static void QuickSort<T>(FixedSizeStack<int> stack, T[] array) where T : IComparable<T>
    {
      int startIndex = 0;
      int endIndex = array.Length - 1;

      stack.Push(startIndex);
      stack.Push(endIndex);

      while (stack.Count > 0)
      {
        endIndex = stack.Pop();
        startIndex = stack.Pop();

        int p = partition(array, startIndex, endIndex);

        // Sort left.
        if (p - 1 > startIndex)
        {
          stack.Push(startIndex);
          stack.Push(p - 1);
        }

        // Sort right.
        if (p + 1 < endIndex)
        {
          stack.Push(p + 1);
          stack.Push(endIndex);
        }
      }
    }

    private static void swap<T>(ref T a, ref T b)
    {
      T t = a;
      a = b;
      b = t;
    }

    private static int partition<T>(T[] array, int start, int end) where T : IComparable<T>
    {
      T x = array[end];
      int i = (start - 1);

      for (int j = start; j <= end - 1; j++)
      {
        if (array[j].CompareTo(x) <= 0)
        {
          i++;
          swap(ref array[i], ref array[j]);
        }
      }
      swap(ref array[i + 1], ref array[end]);
      return (i + 1);
    }

  }
}

