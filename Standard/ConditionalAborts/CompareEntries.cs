
using Bonsai.Core;
using UnityEngine;

namespace Bonsai.Standard
{
  /// <summary>
  /// Compares two values from the blackboard.
  /// </summary>
  [BonsaiNode("Conditional/", "Condition")]
  public class CompareEntries : ConditionalAbort
  {
    public string key1;
    public string key2;

    [Tooltip("If the comparison should test for inequality")]
    public bool compareInequality = false;

    public override bool Condition()
    {
      Blackboard bb = Blackboard;

      // If any of the keys is non-existant then return false.
      if (!bb.Contains(key1) || !bb.Contains(key2))
      {
        return false;
      }

      object val1 = bb.Get(key1);
      object val2 = bb.Get(key2);

      // Use the polymorphic equals so value types are properly tested as well
      // as taking into account custom equality operations.
      bool bResult = val1.Equals(val2);

      return compareInequality ? !bResult : bResult;
    }
  }
}