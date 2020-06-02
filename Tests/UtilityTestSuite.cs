using System.Linq;
using NUnit.Framework;
using Bonsai.Utility;
using NUnit.Framework.Internal;

namespace Tests
{
  public class UtilityTestSuite
  {

    private static int[] randomArray(int size)
    {
      return Enumerable.Range(0, size).Select(i => UnityEngine.Random.Range(-100, 100)).ToArray();
    }

    private bool isAscending(int[] array)
    {
      for (int i = 1; i < array.Length; i++)
      {
        if (array[i - 1] > array[i])
        {
          return false;
        }
      }
      return true;
    }

    [Test]
    public void InsertionSortOrdered()
    {
      int testCount = 10;
      for (int i = 0; i < testCount; i++)
      {
        int arraySize = 16;
        var testArray = randomArray(arraySize);
        Sorts.InsertionSort(testArray);
        Assert.AreEqual(arraySize, testArray.Length);
        Assert.IsTrue(isAscending(testArray));
      }
    }

    [Test]
    public void QuickSortOrdered()
    {
      int testCount = 10;
      for (int i = 0; i < testCount; i++)
      {
        int arraySize = 100;
        var testArray = randomArray(arraySize);
        Sorts.QuickSort(testArray);
        Assert.AreEqual(arraySize, testArray.Length);
        Assert.IsTrue(isAscending(testArray));
      }
    }

    [Test]
    public void FixedSorterOrdered()
    {
      int testCount = 10;
      for (int i = 0; i < testCount; i++)
      {
        int arraySize = i * 10;
        var testArray = randomArray(arraySize);
        var sorter = new FixedSorter<int>(testArray);
        sorter.Sort();
        Assert.AreEqual(arraySize, testArray.Length);
        Assert.IsTrue(isAscending(testArray));
      }
    }
  }
}
